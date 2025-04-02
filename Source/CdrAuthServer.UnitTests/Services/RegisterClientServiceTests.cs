using CdrAuthServer.Configuration;
using CdrAuthServer.Services;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace CdrAuthServer.UnitTests.Services
{
    internal class RegisterClientServiceTests
    {
        private readonly IOptions<CdrRegisterConfiguration> _options = Options.Create(new CdrRegisterConfiguration { Version = 3, GetDataRecipientsEndpoint = "https://localhost/cdr-register/v1/all/data-recipients" });

        private readonly Mock<HttpClient> _mockHttpClient = new();

        [Test]
        public async Task GetDataRecipientsReturnsNullForFailedRequest()
        {
            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
                .Verifiable(Times.Once);

            var service = new RegisterClientService(_mockHttpClient.Object, _options);

            // Act
            var result = await service.GetDataRecipients();

            // Assert
            Assert.IsNull(result);
            _mockHttpClient.VerifyAll();
        }

        [Test]
        public async Task GetDataRecipientsSendsCorrectHeaders()
        {
            HttpRequestHeaders? headers = null;

            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => headers = req.Headers)
                .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.NotImplemented));

            var service = new RegisterClientService(_mockHttpClient.Object, _options);

            // Act
            _ = await service.GetDataRecipients();

            // Assert
            Assert.IsNotNull(headers);
            Assert.IsTrue(headers!.TryGetValues("x-v", out var versionHeader));
            Assert.Contains(_options.Value.Version.ToString(), versionHeader!.ToList());
        }

        [Test]
        public async Task GetDataRecipientsReturnsLegalEntitiesForSuccessfulRequest()
        {
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                        """
                        {
                          "data": [
                            {
                              "legalEntityId": "string",
                              "legalEntityName": "string",
                              "accreditationNumber": "string",
                              "accreditationLevel": "UNRESTRICTED",
                              "logoUri": "string",
                              "dataRecipientBrands": [
                                {
                                  "dataRecipientBrandId": "string",
                                  "brandName": "string",
                                  "logoUri": "string",
                                  "softwareProducts": [
                                    {
                                      "softwareProductId": "string",
                                      "softwareProductName": "string",
                                      "softwareProductDescription": "string",
                                      "logoUri": "string",
                                      "status": "ACTIVE"
                                    }
                                  ],
                                  "status": "ACTIVE"
                                }
                              ],
                              "status": "ACTIVE",
                              "lastUpdated": "string"
                            }
                          ],
                          "links": {
                            "self": "https://localhost/cdr-register/v1/all/data-recipients"
                          },
                          "meta": {}
                        }
                        """),
            };

            httpResponse.Headers.Add("x-v", "3");

            // Arrange
            _mockHttpClient
                .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(httpResponse)
                .Verifiable(Times.Once);

            var service = new RegisterClientService(_mockHttpClient.Object, _options);

            // Act
            var result = await service.GetDataRecipients();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result!.Data.Count());
            Assert.AreEqual("https://localhost/cdr-register/v1/all/data-recipients", result!.Links.Self?.AbsoluteUri);
            _mockHttpClient.VerifyAll();
        }
    }
}
