﻿using System;
using System.Threading.Tasks;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Validators
{
    [TestFixture]
    public class AuthorizeRequestValidatorTests
    {
        private const string RESPONSETYPE = "code";
        private ILogger<AuthorizeRequestValidator> logger = null!;
        private IConfiguration configuration = null!;
        private Mock<IClientService> clientService = null!;
        private Mock<IGrantService> grantService = null!;
        private Mock<ICdrService> cdrService = null!;
        private AuthorizeRequestValidator autherizeRequestValidator = null!;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<AuthorizeRequestValidator>>().Object;
            clientService = new Mock<IClientService>();
            grantService = new Mock<IGrantService>();
            cdrService = new Mock<ICdrService>();

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            autherizeRequestValidator = new AuthorizeRequestValidator(
                logger,
                clientService.Object,
                grantService.Object,
                cdrService.Object);
        }

        [TestCase("", "", RESPONSETYPE, "", "", "client_id is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("foo", "", RESPONSETYPE, "", "", "Invalid client_id", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("fooClient", "", RESPONSETYPE, "", "", "request_uri is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("fooClient", "https://server/uri", RESPONSETYPE, "", "", "Invalid request_uri", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("fooClient", "https://server/uri", RESPONSETYPE, "", "", "request_uri has expired", ErrorCodes.Generic.InvalidRequestUri)]
        [TestCase("fooClient", "https://server/uri", RESPONSETYPE, "", "", "request_uri has already been used", ErrorCodes.Generic.InvalidRequestUri)]
        [TestCase("fooClient", "https://server/uri", RESPONSETYPE, "", "", "client_id does not match request_uri client_id", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "", "", "Invalid redirect_uri for client", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", "", "", "", "response_type is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", "foo", "", "", "response_type is not supported", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "", "", "response_type does not match request_uri response_type", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "", "", "scope is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "", "", "openid scope is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "openid scope", "foo", "response_mode is not supported", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "openid scope", "form_post", "Software product not found", ErrorCodes.Generic.InvalidClient)]
        [TestCase("78273140-cfa2-4073-b248-0eb41940e4c3", "https://server/uri", RESPONSETYPE, "openid scope", "form_post", "Software product status is INACTIVE", ErrorCodes.Cds.AdrStatusNotActive)]
        public async Task Should_Return_InvalidRequest_With_ErrorCodes(
            string client_id,
            string request_uri,
            string response_type,
            string scope,
            string response_mode,
            string expectedErrorDescription,
            string expectedError)
        {
            // Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            var authRequest = new AuthorizeRequest()
            {
                Client_id = client_id,
                Request_uri = request_uri,
                Scope = scope,
                Redirect_uri = "https://dr.dev.cdrsandbox.gov.au/consent/callback",
                Response_type = response_type,
                Nonce = string.Empty,
                Response_mode = response_mode,
            };
            var grantType = "request_uri";

            // ""scope"": ""openid profile common:customer.basic:read bank:accounts.basic:read bank:transactions:read cdr:registration"",
            var requestObjectJson = @"{
                          ""response_type"": ""code"",                          
                          ""client_id"": ""78273140-cfa2-4073-b248-0eb41940e4c3"",
                          ""redirect_uri"": ""https://dr.dev.cdrsandbox.gov.au/consent/callback"",
                          ""response_mode"": ""jwt"",
                          ""scope"": ""openid profile"",
                          ""state"": ""81afd662-b1e7-474b-a01c-8b7fa6ee0b0a"",
                          ""nonce"": ""91d02897-e8e1-4768-bf88-e5cd5018bd0b"",
                          ""claims"": {
                            ""sharing_duration"": 31536000,
                            ""cdr_arrangement_id"": null,
                            ""userinfo"": {
                              ""given_name"": null,
                              ""family_name"": null
                            },
                            ""id_token"": {
                              ""acr"": {
                                ""essential"": true,
                                ""values"": [
                                  ""urn:cds.au:cdr:3""
                                ]
                              }
                            }
                          },
                          ""code_challenge"": ""BNI5VYxaKEpzoGzhDfH51U8oLELng_AJGhjp8vymzlc"",
                          ""code_challenge_method"": ""S256"",
                          ""nbf"": 1663713045,
                          ""exp"": 1663713345,
                          ""iat"": 1663713045,
                          ""iss"": ""78273140-cfa2-4073-b248-0eb41940e4c3"",
                          ""aud"": ""https://dh-bank.idp.dev.cdrsandbox.gov.au""
                        }";

            if (!string.Equals("Invalid client_id", expectedErrorDescription))
            {
                var client = new Client() { ClientId = authRequest.Client_id, Scope = authRequest.Scope, SoftwareId = "77773140-cfa2-4073-b248-0eb51540e5c6" };
                _ = clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);

                if (string.Equals("Invalid request_uri", expectedErrorDescription))
                {
                    _ = grantService.Setup(x => x.Get(grantType, authRequest.Request_uri, authRequest.Client_id)).ReturnsAsync(value: null);
                }
                else
                {
                    var requestUriGrant = new RequestUriGrant() { GrantType = grantType, ClientId = authRequest.Client_id };

                    if (string.Equals("request_uri has expired", expectedErrorDescription))
                    {
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(-30);
                    }

                    if (string.Equals("request_uri has already been used", expectedErrorDescription))
                    {
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.UsedAt = DateTime.UtcNow;
                    }

                    if (string.Equals("client_id does not match request_uri client_id", expectedErrorDescription) ||
                        string.Equals("Invalid redirect_uri for client", expectedErrorDescription))
                    {
                        client.RedirectUris = ["http://server/redirecturi1", "http://server/redirecturi2"];
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("response_type is missing", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestObjectJson = requestObjectJson.Replace("code", string.Empty);
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("response_type is not supported", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestObjectJson = requestObjectJson.Replace("code", "foo");
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("response_type does not match request_uri response_type", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                        authRequest.Response_type = "foo";
                    }

                    if (string.Equals("scope is missing", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestObjectJson = requestObjectJson.Replace("openid profile", string.Empty);
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("openid scope is missing", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestObjectJson = requestObjectJson.Replace("openid profile", "profile");
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("response_mode is not supported", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestObjectJson = requestObjectJson.Replace("jwt", "foo");
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("Software product not found", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;
                    }

                    if (string.Equals("Software product status is INACTIVE", expectedErrorDescription))
                    {
                        client.RedirectUris = ["https://dr.dev.cdrsandbox.gov.au/consent/callback", "http://server/redirecturi2"];
                        requestUriGrant.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                        requestUriGrant.Request = requestObjectJson;

                        var softwareProduct = new SoftwareProduct()
                        {
                            SoftwareProductId = "77773140-cfa2-4073-b248-0eb51540e5c6",
                            BrandName = "Foo brand name",
                            BrandStatus = "ACTIVE",
                            Status = "ACTIVE",
                            LegalEntityStatus = "INACTIVE",
                        };

                        _ = cdrService.Setup(x => x.GetSoftwareProduct(client.SoftwareId)).ReturnsAsync(softwareProduct);
                    }

                    _ = grantService.Setup(x => x.Get(grantType, authRequest.Request_uri, authRequest.Client_id)).ReturnsAsync(requestUriGrant);
                }
            }

            // Act
            var result = await autherizeRequestValidator.Validate(authRequest, configOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(expectedError, result.Error);
            Assert.IsTrue(result.ErrorDescription?.Contains(expectedErrorDescription));
        }
    }
}
