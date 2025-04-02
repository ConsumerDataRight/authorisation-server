using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using CdrAuthServer.Configuration;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;

namespace CdrAuthServer.UnitTests.Services
{
    internal class ConsentRevocationServiceTests
    {
        private readonly IOptions<ConfigurationOptions> _configurationOptions = Options.Create(new ConfigurationOptions { BrandId = Guid.NewGuid().ToString() });

        private readonly Mock<HttpClient> _mockHttpClient = new();

        private readonly Mock<ILogger<ConsentRevocationService>> _logger = new();

        private readonly Mock<ICertificateLoader> _certificateLoader = new();

        private readonly Client _client = new() { ClientId = Guid.NewGuid().ToString(), RecipientBaseUri = "http://localhost" };

        private readonly string _arrangementId = Guid.NewGuid().ToString();

        private readonly X509Certificate2 _ps256SigningCertificate = CertificateHelper.CreateSigning();

        public ConsentRevocationServiceTests()
        {
            _certificateLoader.Setup(x => x.Load(It.IsAny<CertificateLoadDetails>())).ReturnsAsync(_ps256SigningCertificate);
        }

        /// <summary>
        /// Ensure that when the request is successfully sent the appropriate request/response pair is returned.
        /// </summary>
        /// <remarks>Expect no exception, only a request and response.</remarks>
        [Test]
        public async Task RevokeAdrArrangementReturnsRequestResponsePairForSuccessfulRequest()
        {
            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent)
                {
                    Content = new StringContent(string.Empty),
                });

            var service = new ConsentRevocationService(_mockHttpClient.Object, _configurationOptions, _certificateLoader.Object, _logger.Object);

            // Act
            var (request, response, exception) = await service.RevokeAdrArrangement(_client, _arrangementId, TimeSpan.FromSeconds(5));

            // Assert
            Assert.NotNull(request);
            Assert.NotNull(response);
            Assert.IsNull(exception);

            await AssertBearerTokenIsValid(request);
            await AssertArrangementIsValid(request);
        }

        /// <summary>
        /// Ensure that when an exception is thrown that is related to cancellation that it is handled appropriately.
        /// </summary>
        /// <remarks>Expect an appropriate <c>Exception</c> to be returned, and no response.</remarks>
        [Test]
        public async Task RevokeAdrArrangementReturnsRequestExceptionPairForThrownException()
        {
            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException("Emulate an exception thrown that isn't due to cancellation"));

            var service = new ConsentRevocationService(_mockHttpClient.Object, _configurationOptions, _certificateLoader.Object, _logger.Object);

            // Act
            var (request, response, exception) = await service.RevokeAdrArrangement(_client, _arrangementId, TimeSpan.FromSeconds(5));

            // Assert
            Assert.NotNull(request);
            Assert.NotNull(request.Content);
            Assert.IsNull(response);
            Assert.NotNull(exception);
        }

        /// <summary>
        /// Ensure that when the request is not responded to by the time the timeout expires it is cancelled.
        /// </summary>
        /// <remarks>Expect a <c>TaskCancellationException</c> to be returned, and no response.</remarks>
        [Test]
        public async Task RevokeAdrArrangementCancelsOnTimeout()
        {
            var stopWatch = new Stopwatch();

            // The round trip to be more than the call timeout so that the task gets cancelled.
            int roundTripDurationMs = 500, callTimeoutMs = 200;

            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns<HttpRequestMessage, CancellationToken>(async (message, token) =>
                {
                    // Wait until the token is cancelled
                    await Task.Delay(roundTripDurationMs, token);
                    return new HttpResponseMessage();
                });

            var service = new ConsentRevocationService(_mockHttpClient.Object, _configurationOptions, _certificateLoader.Object, _logger.Object);

            // Act
            stopWatch.Start();
            var (request, response, exception) = await service.RevokeAdrArrangement(_client, _arrangementId, TimeSpan.FromMilliseconds(callTimeoutMs));
            stopWatch.Stop();

            // Assert
            Assert.NotNull(request);
            Assert.NotNull(request.Content);
            Assert.IsNull(response);
            Assert.NotNull(exception);
            Assert.IsAssignableFrom(typeof(TaskCanceledException), exception);
            Assert.LessOrEqual(stopWatch.ElapsedMilliseconds, roundTripDurationMs, "Request is expected to be cancelled after {0} but before {1}", callTimeoutMs, roundTripDurationMs);
        }

        /// <summary>
        /// Ensure that when the parent cancellation token (i.e. incoming token from the caller) is cancelled the request is cancelled.
        /// </summary>
        /// <remarks>Expect a <c>TaskCancellationException</c> to be returned, and no response.</remarks>
        [Test]
        public async Task RevokeAdrArrangementCancelsOnParent()
        {
            var stopWatch = new Stopwatch();

            // The round trip to be less than the call timeout but longer than the parent token timeout in order for the parent cancellation token to cancel first.
            int roundTripDurationMs = 300, callTimeoutMs = 500, requestorTimeoutMs = 100;

            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Returns<HttpRequestMessage, CancellationToken>(async (message, token) =>
                {
                    // Wait until the token is cancelled
                    await Task.Delay(roundTripDurationMs, token);
                    return new HttpResponseMessage();
                });

            var service = new ConsentRevocationService(_mockHttpClient.Object, _configurationOptions, _certificateLoader.Object, _logger.Object);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(requestorTimeoutMs);

            // Act
            stopWatch.Start();
            var (request, response, exception) = await service.RevokeAdrArrangement(_client, _arrangementId, TimeSpan.FromSeconds(callTimeoutMs), cts.Token);
            stopWatch.Stop();

            // Assert
            Assert.NotNull(request);
            Assert.NotNull(request.Content);
            Assert.IsNull(response);
            Assert.NotNull(exception);
            Assert.IsAssignableFrom(typeof(TaskCanceledException), exception);
            Assert.LessOrEqual(stopWatch.ElapsedMilliseconds, roundTripDurationMs, "Request is expected to be cancelled after {0} but before {1}", requestorTimeoutMs, callTimeoutMs);
        }

        /// <summary>
        /// Ensure that the bearer token sent has the expected details and signature.
        /// </summary>
        private async Task AssertBearerTokenIsValid(HttpRequestMessage request)
        {
            var handler = new JsonWebTokenHandler();
            var publicKey = new X509Certificate2(_ps256SigningCertificate.GetRawCertData());

            var authHeader = request.Headers.Authorization;

            Assert.NotNull(authHeader);
            Assert.AreEqual("Bearer", authHeader!.Scheme);

            var validationResult = await handler.ValidateTokenAsync(
                                                    authHeader.Parameter,
                                                    new TokenValidationParameters
                                                    {
                                                        IssuerSigningKey = new X509SecurityKey(publicKey),
                                                        ValidAudience = _client.RecipientBaseUri + "/arrangements/revoke",
                                                        ValidIssuer = _configurationOptions.Value.BrandId,
                                                        ValidateIssuerSigningKey = true,
                                                        ValidateIssuer = true,
                                                        ValidateAudience = true,
                                                        ValidateLifetime = true,
                                                        ValidateTokenReplay = true,
                                                    });

            Assert.IsTrue(validationResult.IsValid, validationResult.Exception != null ? validationResult.Exception.Message : string.Empty);

            var subjectIsValid = validationResult.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var subject) && (string)subject == _configurationOptions.Value.BrandId;
            var jtiIsValid = validationResult.Claims.TryGetValue(JwtRegisteredClaimNames.Jti, out var jti) && !string.IsNullOrEmpty((string)jti);

            Assert.IsTrue(subjectIsValid, $"Claim 'subject' was not provided or did not match {_configurationOptions.Value.BrandId}");
            Assert.IsTrue(jtiIsValid, "Claim 'jti' must be provided");
        }

        /// <summary>
        /// Ensure that the arrangement has the expected details and signature.
        /// </summary>
        private async Task AssertArrangementIsValid(HttpRequestMessage request)
        {
            var handler = new JsonWebTokenHandler();
            var publicKey = new X509Certificate2(_ps256SigningCertificate.GetRawCertData());

            Assert.AreEqual("application/x-www-form-urlencoded", request.Content?.Headers.ContentType?.MediaType);

            using var reader = new Microsoft.AspNetCore.WebUtilities.FormReader(await request.Content!.ReadAsStreamAsync());
            var formValues = await reader.ReadFormAsync();

            Assert.True(formValues.TryGetValue("cdr_arrangement_jwt", out StringValues cdrArrangementJwt));

            var validationResult = await handler.ValidateTokenAsync(cdrArrangementJwt, new TokenValidationParameters
            {
                ValidIssuer = _configurationOptions.Value.BrandId,
                ValidAudience = _client.RecipientBaseUri + "/arrangements/revoke",
                IssuerSigningKey = new X509SecurityKey(publicKey),
            });

            Assert.IsTrue(validationResult.IsValid, validationResult.Exception != null ? validationResult.Exception.Message : string.Empty);
            Assert.AreEqual(_arrangementId, validationResult.Claims.FirstOrDefault(x => x.Key == "cdr_arrangement_id").Value);
        }
    }
}
