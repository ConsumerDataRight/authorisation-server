using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CdrAuthServer.Controllers;
using CdrAuthServer.Models;
using CdrAuthServer.Models.Json;
using CdrAuthServer.Models.Register;
using CdrAuthServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Controllers
{
    internal class AdminControllerTests
    {
        private readonly Mock<ILogger<AdminController>> _logger = new();
        private readonly Mock<IRegisterClientService> _registerClientService = new();
        private readonly Mock<IClientService> _clientService = new();
        private readonly Mock<ICdrService> _cdrService = new();

        [Test]
        public async Task RefreshDataRecipientsReturnsUnauthorizedForInvalidClient()
        {
            var context = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimNames.ClientId, "invalid_client")])]),
                },
            };

            var controller = new AdminController(_cdrService.Object, _clientService.Object, _logger.Object, _registerClientService.Object)
            {
                ControllerContext = context,
            };

            var request = new DataRecipientRequest { Data = new Data { Action = "REFRESH" } };

            var result = await controller.RefreshDataRecipients(request, default);

            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            Assert.NotNull(controller.HttpContext.Response.Headers.WWWAuthenticate);
        }

        [Test]
        public async Task RefreshDataRecipientsReturnsInternalErrorWhenRefreshFails()
        {
            var clientId = "valid";
            var context = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimNames.ClientId, clientId)])]),
                },
            };

            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync(new Client());

            var controller = new AdminController(_cdrService.Object, _clientService.Object, _logger.Object, _registerClientService.Object)
            {
                ControllerContext = context,
            };

            var request = new DataRecipientRequest { Data = new Data { Action = "REFRESH" } };

            var result = await controller.RefreshDataRecipients(request, default);
            _registerClientService.Verify(x => x.GetDataRecipients(default), Times.Once);

            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var converted);
            Assert.AreEqual(StatusCodes.Status500InternalServerError, converted.StatusCode);
            Assert.AreEqual("Data recipient data could not be refreshed.", converted.Value);
        }

        [Test]
        public async Task RefreshDataRecipientsReturnsInternalErrorWhenRequestActionUnsupported()
        {
            var clientId = "valid";
            var context = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimNames.ClientId, clientId)])]),
                },
            };

            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync(new Client());

            var controller = new AdminController(_cdrService.Object, _clientService.Object, _logger.Object, _registerClientService.Object)
            {
                ControllerContext = context,
            };

            var request = new DataRecipientRequest { Data = new Data { Action = "WRONG'N" } };

            var result = await controller.RefreshDataRecipients(request, default);
            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var converted);
            Assert.AreEqual(StatusCodes.Status500InternalServerError, converted.StatusCode);
            Assert.AreEqual("Data recipient data could not be refreshed.", converted.Value);
        }

        [Test]
        public async Task RefreshDataRecipientsReturnsOkWhenRefreshSucceeds()
        {
            var clientId = "valid";
            var self = "http://inception/cdr-register/v1/all/data-recipients";
            var context = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimNames.ClientId, clientId)])]),
                },
            };

            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync(new Client());
            _registerClientService.Setup(x => x.GetDataRecipients(default)).ReturnsAsync(new RegisterResponse<LegalEntity> { Data = GenerateEntities(3), Links = new Links { Self = new Uri(self) } });

            var controller = new AdminController(_cdrService.Object, _clientService.Object, _logger.Object, _registerClientService.Object)
            {
                ControllerContext = context,
            };

            var request = new DataRecipientRequest { Data = new Data { Action = "REFRESH" } };

            var result = await controller.RefreshDataRecipients(request, default);
            _cdrService.Verify(x => x.PurgeDataRecipients(), Times.Once);
            _cdrService.Verify(x => x.InsertDataRecipients(It.IsAny<List<SoftwareProduct>>()), Times.Once);
            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var converted);
            Assert.AreEqual(StatusCodes.Status200OK, converted.StatusCode);
            Assert.AreEqual($"Data recipient records refreshed from {self}.", converted.Value);
        }

        [Test]
        public async Task RefreshDataRecipientsReturnsInternalErrorWhenThereAreNoSoftwareProducts()
        {
            var clientId = "valid";
            var self = "http://inception/cdr-register/v1/all/data-recipients";
            var context = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimNames.ClientId, clientId)])]),
                },
            };

            _clientService.Setup(x => x.Get(clientId)).ReturnsAsync(new Client());
            _registerClientService.Setup(x => x.GetDataRecipients(default)).ReturnsAsync(new RegisterResponse<LegalEntity> { Data = GenerateEntities(0), Links = new Links { Self = new Uri(self) } });

            var controller = new AdminController(_cdrService.Object, _clientService.Object, _logger.Object, _registerClientService.Object)
            {
                ControllerContext = context,
            };

            var request = new DataRecipientRequest { Data = new Data { Action = "REFRESH" } };

            var result = await controller.RefreshDataRecipients(request, default);
            _cdrService.Verify(x => x.PurgeDataRecipients(), Times.Once);
            _cdrService.Verify(x => x.InsertDataRecipients(It.IsAny<List<SoftwareProduct>>()), Times.Never);

            ResultHelper.AssertInstanceOf<ObjectResult>(result, out var converted);
            Assert.AreEqual(StatusCodes.Status500InternalServerError, converted.StatusCode);
            Assert.AreEqual("Data recipient data could not be refreshed.", converted.Value);
        }

        [TearDown]
        public void Reset()
        {
            _cdrService.Reset();
            _clientService.Reset();
            _registerClientService.Reset();
        }

        private IEnumerable<LegalEntity> GenerateEntities(int count)
        {
            for (var i = 1; i <= count; i++)
            {
                var legalEntityId = Guid.NewGuid().ToString();
                yield return new LegalEntity
                {
                    LegalEntityId = legalEntityId,
                    LegalEntityName = $"Test Entity {i}",
                    Status = "Active",
                    DataRecipientBrands = [new DataRecipientBrand
                    {
                        BrandName = $"Brand {i}",
                        DataRecipientBrandId = i.ToString(),
                        Status = "Active",
                        SoftwareProducts = [new SoftwareProduct
                        {
                            LegalEntityId = legalEntityId,
                            LegalEntityName = $"Test Entity {i}",
                            LegalEntityStatus = "Active",
                            SoftwareProductId = Guid.NewGuid().ToString(),
                            SoftwareProductDescription = $"Description {i}",
                            Status = "Active",
                            LogoUri = "http://inception/mylogo.png",
                        }
                        ],
                    }
                    ],
                };
            }
        }
    }
}
