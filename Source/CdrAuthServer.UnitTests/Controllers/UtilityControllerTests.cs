using CdrAuthServer.Controllers;
using CdrAuthServer.Domain.Models;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Controllers
{
    /// <summary>
    /// Tests for <see cref="UtilityController"/>.
    /// </summary>
    internal class UtilityControllerTests
    {
        private const string _errorResponse = """
            {
                "errors": [
                {
                    "code": "urn:au-cds:error:cds-all:Authorisation/InvalidArrangement",
                    "title": "Unable to process revocation",
                    "detail": "The arrangement is invalid",
                }
                ]
            }
            """;

        private readonly Mock<ILogger<UtilityController>> _logger = new();
        private readonly Mock<IGrantService> _grantService = new();
        private readonly Mock<IClientService> _clientService = new();
        private readonly Mock<IConsentRevocationService> _consentRevocationService = new();

        [Test]
        public async Task RemoveArrangementReturnsBadRequestForMissingCdrArrangementId()
        {
            // Arrange
            var cdrArrangementId = string.Empty;
            var controller = new UtilityController(_logger.Object, _grantService.Object, _clientService.Object, _consentRevocationService.Object);

            // Act
            var result = await controller.RemoveArrangementAndTriggerDataRecipientArrangementRevocation(cdrArrangementId, default);

            // Assert
            ResultHelper.AssertInstanceOf<BadRequestObjectResult>(result, out var badRequest);
            ResultHelper.AssertErrorExpectation(badRequest, ErrorCodes.Cds.InvalidField, "Invalid Field", "cdrArrangementId");
        }

        [Test]
        public async Task RemoveArrangementReturnsBadRequestForCdrArrangementIdNotFound()
        {
            // Arrange
            var cdrArrangementId = Guid.NewGuid().ToString();
            _grantService.Setup(x => x.Get(GrantTypes.CdrArrangement, cdrArrangementId, null)).ReturnsAsync((Grant?)null);
            var controller = new UtilityController(_logger.Object, _grantService.Object, _clientService.Object, _consentRevocationService.Object);

            // Act
            var result = await controller.RemoveArrangementAndTriggerDataRecipientArrangementRevocation(cdrArrangementId, default);

            // Assert
            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var badRequest);
            Assert.AreEqual((int)HttpStatusCode.BadRequest, badRequest.StatusCode);
            ResultHelper.AssertErrorExpectation(badRequest, ErrorCodes.Cds.InvalidField, "Invalid Field", "cdrArrangementId");
        }

        [Test]
        public async Task RemoveArrangementReturnsInternalServerErrorForGrantNotFound()
        {
            // Arrange
            var cdrArrangementId = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid().ToString();

            _grantService.Setup(x => x.Get(GrantTypes.CdrArrangement, cdrArrangementId, null)).ReturnsAsync(new Grant { ClientId = clientId });
            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync((Client?)null);
            var controller = new UtilityController(_logger.Object, _grantService.Object, _clientService.Object, _consentRevocationService.Object);

            // Act
            var result = await controller.RemoveArrangementAndTriggerDataRecipientArrangementRevocation(cdrArrangementId, default);

            // Assert
            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var objResult);
            Assert.AreEqual((int)HttpStatusCode.InternalServerError, objResult.StatusCode);
            ResultHelper.AssertErrorExpectation(objResult, ErrorCodes.Cds.InvalidConsentArrangement, "Invalid client_id", clientId);
        }

        [TestCase(typeof(TaskCanceledException), "Message", "The operation was cancelled as the ADR did not respond within the timeout period of 30 seconds.")]
        [TestCase(typeof(NotImplementedException), "Something else went wrong in client call", "Something else went wrong in client call")]
        public async Task RemoveArrangementReturnsOKWithExceptionMessage(Type exceptionType, string exceptionMessage, string expectedResponseContent)
        {
            // Arrange
            var cdrArrangementId = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid().ToString();
            _grantService.Setup(x => x.Get(GrantTypes.CdrArrangement, cdrArrangementId, null)).ReturnsAsync(new CdrArrangementGrant { ClientId = clientId });
            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync(new Client { ClientId = clientId });
            _consentRevocationService
                .Setup(x => x.RevokeAdrArrangement(It.IsAny<Client>(), cdrArrangementId, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IConsentRevocationService.OutboundCallDetails(GenerateRequestMessage(), null, (Exception)Activator.CreateInstance(exceptionType, exceptionMessage)!));

            var controller = new UtilityController(_logger.Object, _grantService.Object, _clientService.Object, _consentRevocationService.Object);

            // Act
            var result = await controller.RemoveArrangementAndTriggerDataRecipientArrangementRevocation(cdrArrangementId, default);

            // Assert
            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var objResult);
            ResultHelper.AssertJsonInstanceOf<AdrArrangementRevocationResponse>(objResult!, out var response);

            AssertRequestIsPopulated(response.ArrangeRevocationRequest);

            // Response should be custom error message
            Assert.NotNull(response.ArrangeRevocationResponse);
            Assert.AreEqual(expectedResponseContent, response.ArrangeRevocationResponse!.Content);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task RemoveArrangementReturnsOkForSuccessfullySendRequest(bool error)
        {
            // Arrange
            var cdrArrangementId = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid().ToString();
            _grantService.Setup(x => x.Get(GrantTypes.CdrArrangement, cdrArrangementId, null)).ReturnsAsync(new CdrArrangementGrant { ClientId = clientId });
            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync(new Client { ClientId = clientId });
            _consentRevocationService
                .Setup(x => x.RevokeAdrArrangement(It.IsAny<Client>(), cdrArrangementId, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IConsentRevocationService.OutboundCallDetails(GenerateRequestMessage(), error ? GenerateErrorResponseMessage() : GenerateResponseMessage(), null));

            var controller = new UtilityController(_logger.Object, _grantService.Object, _clientService.Object, _consentRevocationService.Object);

            // Act
            var result = await controller.RemoveArrangementAndTriggerDataRecipientArrangementRevocation(cdrArrangementId, default);

            // Assert
            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var objResult);
            ResultHelper.AssertJsonInstanceOf<AdrArrangementRevocationResponse>(objResult!, out var response);

            AssertRequestIsPopulated(response.ArrangeRevocationRequest);
            AssertResponseIsPopulated(response.ArrangeRevocationResponse, error);
        }

        private static HttpRequestMessage GenerateRequestMessage()
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://localhost/arrangements/revoke"),
                Method = HttpMethod.Post,
                Content = new StringContent(
                    "cdr_arrangement_jwt=eyJhbGciOiJQUzI1NiIsImtpZCI6IkQzRjc3MzE1RjExOTJFOEZFNDhDRjIwMkVBOTU5REYzQzIwNkQ4QTgiLCJ4NXQiOiIwX2R6RmZFWkxvX2tqUElDNnBXZDg4SUcyS2ciLCJ0eXAiOiJKV1QifQ.eyJhdWQiOiJodHRwOi8vbG9jYWxob3N0L2FycmFuZ2VtZW50cy9yZXZva2UiLCJpc3MiOiI4YmM5MTgyMi05Y2Q2LTRkYmMtYTg3Yi01OWZmY2RhODQwNzAiLCJleHAiOjE3Mjk0Nzg5OTgsImNkcl9hcnJhbmdlbWVudF9pZCI6IjE2N2IzZDYxLTM0ZTYtNDI4YS04Zjc2LTM2Zjg1YjQ1MzA5ZiIsInN1YiI6IjhiYzkxODIyLTljZDYtNGRiYy1hODdiLTU5ZmZjZGE4NDA3MCIsImp0aSI6IjA5MjlkM2VlLTI1YTItNGRkNS1iOGEwLWYzZDMyMTQ3MzZmNiIsImlhdCI6MTcyOTQ3ODY5OCwibmJmIjoxNzI5NDc4Njk4fQ.cAaaI_Cu7jARBROJcMmMlJX0yh7dSRzkuDVLf4d8pHHV46tO4XGZIqfl-nTF8tpysq2CZsl0BhAXZzC-N4rdUtcq90Qgv5sYHCgFZzfx4v8xxeywWNxegfCyjIxk5dhe0hu4qh3Izq-lmEgpAbI5TbI66bBwaJl0m8ZC3-1DXKV9Ddbq3k9IwLya5gkwp6Hm1gRhCN9SMiZoTeTvqhj3JCHKclWUsYvodYUncFqh9U9Zm51OlxlXgvSs6z3yJfVnYEIomyvD62aSNXTQWteUA2dnP3auBhhLAOvXO5Obk2M383wz0VDRVhXsMj3WdTZQgdULTJiP4KwosM8Kqn7PnQ",
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")),
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJhbGciOiJQUzI1NiIsImtpZCI6IkQzRjc3MzE1RjExOTJFOEZFNDhDRjIwMkVBOTU5REYzQzIwNkQ4QTgiLCJ4NXQiOiIwX2R6RmZFWkxvX2tqUElDNnBXZDg4SUcyS2ciLCJ0eXAiOiJKV1QifQ.eyJhdWQiOiJodHRwOi8vbG9jYWxob3N0L2FycmFuZ2VtZW50cy9yZXZva2UiLCJpc3MiOiI4YmM5MTgyMi05Y2Q2LTRkYmMtYTg3Yi01OWZmY2RhODQwNzAiLCJleHAiOjE3Mjk0Nzg5OTgsInN1YiI6IjhiYzkxODIyLTljZDYtNGRiYy1hODdiLTU5ZmZjZGE4NDA3MCIsImp0aSI6ImE0MjVkN2QzLTA0ZGItNGYzMi04NjM5LTFmYzFkNmNkODVlYSIsImlhdCI6MTcyOTQ3ODY5OCwibmJmIjoxNzI5NDc4Njk4fQ.lZJxKCHUiDITFNLNLdzaakaiQStx0kq2RGe4HQK6ImhyUfU3-wU6Obdy7bVyWH986DvWHhMm5BTuRW6g2BJ25VqrSHlweCUJ9wJgLmiwTnzr3h33N3cuoFdReos4eraOufdNTwwrvyj60wyBUpMno36DSz_lBPCKOIFriDoa0JtfDWPhJYQaWrjIBk45awLD2GcpeWb6ZIcf2XVdzxF3f6eK7vMtSHkle4qGa4xbbXMzhOogAgYk1KsKcWYSalwB0FAsfVnRKRGtin5T2x-V9znjNthlR0JtfwilM_rN4NRTMl0a9156t4Phd3C9Z-KVrLXDIHrjvWoF2w2468a_vA");

            return request;
        }

        private static HttpResponseMessage GenerateResponseMessage()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent,
            };

            response.Headers.Add("Version", ["1.0"]);

            return response;
        }

        private static HttpResponseMessage GenerateErrorResponseMessage()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.UnprocessableEntity,
                Content = new StringContent(_errorResponse),
            };

            return response;
        }

        private static void AssertRequestIsPopulated(ArrangeRevocationRequest? arrangeRevocationRequest)
        {
            Assert.NotNull(arrangeRevocationRequest);
            Assert.NotNull(arrangeRevocationRequest!.Body);
            Assert.That(Regex.IsMatch(arrangeRevocationRequest!.Body!, "cdr_arrangement_jwt=[\\w\\.]+"));
            Assert.AreEqual("POST", arrangeRevocationRequest.Method);
            Assert.AreEqual("https://localhost/arrangements/revoke", arrangeRevocationRequest.Url);
            Assert.IsNotEmpty(arrangeRevocationRequest.Headers);
            ResultHelper.AssertJsonInstanceOf<Dictionary<string, string[]>>(arrangeRevocationRequest.Headers!, out var headers);

            Assert.IsTrue(headers.TryGetValue("Authorization", out var authHeaderValues));
            Assert.AreEqual(1, authHeaderValues!.Count());
            Assert.That(Regex.IsMatch(authHeaderValues![0], "Bearer [\\w\\.]+"));
        }

        private static void AssertResponseIsPopulated(ArrangeRevocationResponse? arrangeRevocationResponse, bool error)
        {
            Assert.NotNull(arrangeRevocationResponse);

            if (error)
            {
                Assert.AreEqual((int)HttpStatusCode.UnprocessableEntity, arrangeRevocationResponse!.StatusCode);
                Assert.AreEqual(_errorResponse, arrangeRevocationResponse.Content);
            }
            else
            {
                Assert.AreEqual((int)HttpStatusCode.NoContent, arrangeRevocationResponse!.StatusCode);
            }
        }
    }
}
