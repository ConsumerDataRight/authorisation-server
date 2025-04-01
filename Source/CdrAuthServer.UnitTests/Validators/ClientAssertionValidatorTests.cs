using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Validators
{
    public class ClientAssertionValidatorTests
    {
        private const string ClientAssertion = @"eyJraWQiOiJCNTQ4QzkxNEEwMjc4N0EzQjVGMTU1ODNDOEVCMDMwRDk0QkMyNDI0IiwiYWxnIjoiUFMyNTYifQ.eyJzdWIiOiIzZTZjNWYzZC1iZDU4LTRhYWEtOGMyMy1hY2ZlYzgzN2I1MDYiLCJhdWQiOiJodHRwczpcL1wvZGgtdGVzdC5pZHAuZGV2LmNkcnNhbmRib3guZ292LmF1XC9kaC1lbmVyZ3ktNSIsImlzcyI6IjNlNmM1ZjNkLWJkNTgtNGFhYS04YzIzLWFjZmVjODM3YjUwNiIsImV4cCI6MTY1MjM0MDUzOCwiaWF0IjoxNjUyMzQwNDc4LCJqdGkiOiJMd3J0YTJLU2RhNGpPWVYwSDVwUiJ9.SjGr9X5vxnYywoVU1GAcG6N4taPniDJPYuEme1wPD2tvNjK4D-huQsb4BuaLJZem1MBbIDZprmvMk8_YkL50qOdvdaFYflqIif6SfFlaAIzN5B-9pzSM3iOC7Q0bt26xjr-C8MZaprc3O7LhsdpSynWIWiqle9I248-quikMsqyTDXhiVm_PtKnDs-DwzdfXvcp4JJcgN4Gk_fb431n2UGeQzFHAT-SCasvDVO7i9Zhw72bS8orWo7-ybiAUFjK8-B38lCih6LZg7mjDQdJWnXmkO4tqTYCIJgVEQteiaxUJRmsPlPX6Uvh0jC22pj3VTqGRIW4yukzeKgtB4q2HyQ";
        public const string SOFTWAREPRODUCT_ID = "c6327f87-687a-4369-99a4-eaacd3bb8210";
        public const string JWT_CERTIFICATE_FILENAME = "Certificates/MDR/jwks.pfx";
        public const string JWT_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";
        public static string DH_MTLS_GATEWAY_URL;

        private Mock<ILogger<ClientAssertionValidator>> logger = null!;
        private Mock<IClientService> clientService = null!;
        private Mock<ITokenService> tokenService = null!;
        private Mock<IJwtValidator> jwtValidator = null!;
        private IConfiguration configuration = null!;
        private ClientAssertionValidator clientAssertionValidator = null!;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<ClientAssertionValidator>>();
            clientService = new Mock<IClientService>();
            tokenService = new Mock<ITokenService>();
            jwtValidator = new Mock<IJwtValidator>();

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            DH_MTLS_GATEWAY_URL = configuration["DHSecureInfosecBaseUri:DH_MTLS_Gateway"] ?? string.Empty;

            clientAssertionValidator = new ClientAssertionValidator(logger.Object, clientService.Object, tokenService.Object, jwtValidator.Object);
        }

        [TestCase("", "", "", "", "", false, false, "client_assertion not provided", ErrorCodes.Generic.InvalidClient)]
        [TestCase("foo", "", "", "", "", false, false, "client_assertion_type not provided", ErrorCodes.Generic.InvalidClient)]
        [TestCase("foo", "foo", "", "", "", false, false, "client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer", ErrorCodes.Generic.InvalidClient)]
        [TestCase("foo", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", "", "", "", true, false, "grant_type not provided", ErrorCodes.Generic.UnsupportedGrantType)]
        [TestCase("foo", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", "", "foo", "", true, false, "unsupported grant_type", ErrorCodes.Generic.UnsupportedGrantType)]
        [TestCase("foo", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", "", "authorization_code", "", true, false, "Cannot read client_assertion.  Invalid format.", ErrorCodes.Generic.InvalidClient)] // grant type: "authorization_code", "client_credentials", "refresh_token"
        [TestCase(ClientAssertion, "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", "", "authorization_code", "", true, false, "Client not found", ErrorCodes.Generic.InvalidClient)]
        [TestCase("validate_assertion1", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", "", "authorization_code", "", true, false, "Client not found", ErrorCodes.Generic.InvalidClient)]
        [TestCase("validate_assertion2", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", "", "authorization_code", "", true, false, "Client not found", ErrorCodes.Generic.InvalidClient)]

        // [TestCase("validate_assertion3", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer", SOFTWAREPRODUCT_ID, "authorization_code", "", true, false, "Client not found", ErrorCodes.Generic.InvalidClient)]
        public async Task Validate_ClientAssertionRequest_InvalidClient_Test(
            string? clientAssertion,
            string? clientAssertionType,
            string? clientId,
            string? grantType,
            string? scope,
            bool isTokenEndpoint,
            bool isValid,
            string expectedErrorDescription,
            string expectedError)
        {
            // Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            var clientAssertionRequest = new ClientAssertionRequest()
            {
                ClientAssertion = clientAssertion,
                ClientAssertionType = clientAssertionType,
                ClientId = clientId,
                GrantType = grantType,
                Scope = scope,
            };

            var errorFailure = ValidationResult.Fail(expectedError, expectedErrorDescription);

            if (string.Equals("validate_assertion1", clientAssertionRequest.ClientAssertion) ||
                string.Equals("validate_assertion2", clientAssertionRequest.ClientAssertion) ||
                string.Equals("validate_assertion3", clientAssertionRequest.ClientAssertion))
            {
                if (string.Equals("validate_assertion2", clientAssertionRequest.ClientAssertion))
                {
                    clientService.Setup(x => x.Get(clientId)).ReturnsAsync(value: null as CdrAuthServer.Models.Client);
                }

                if (string.Equals("validate_assertion3", clientAssertionRequest.ClientAssertion))
                {
                    Models.Client client = new Models.Client() { ClientId = clientAssertionRequest.ClientId };
                    clientService.Setup(x => x.Get(clientId)).ReturnsAsync(client);
                }

                string ISSUER = SOFTWAREPRODUCT_ID.ToLower();

                var now = DateTime.UtcNow;

                var additionalClaims = new List<Claim>
                {
                     new Claim("sub", ISSUER),
                     new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer),
                };

                var expires = now.AddMinutes(10);
                string? aud = null;

                aud = $"{DH_MTLS_GATEWAY_URL}/connect/token";

                var certificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var x509SigningCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);
                var jwt = new JwtSecurityToken(
                    ISSUER,
                    aud,
                    additionalClaims,
                    expires: expires,
                    signingCredentials: x509SigningCredentials);

                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

                clientAssertionRequest.ClientAssertion = jwtSecurityTokenHandler.WriteToken(jwt);
            }

            // Act
            var result = await clientAssertionValidator.ValidateClientAssertionRequest(clientAssertionRequest, configOptions, isTokenEndpoint);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Result.IsValid, isValid);
            Assert.AreEqual(result.Result.Error, errorFailure.Error);
            Assert.IsTrue(result.Result.ErrorDescription?.Contains(expectedErrorDescription));
        }
    }
}
