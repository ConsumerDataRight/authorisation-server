using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Validators
{
    public class JwtValidatorTest : BaseTest
    {
        private Mock<ILogger<JwtValidator>> logger = null!;
        private Mock<IClientService> clientService = null!;
        private IConfiguration configuration = null!;
        private IJwtValidator JwtValidator = null!;
        private (ValidationResult, JwtSecurityToken) mockJwtValidatorResult;

        public Client client { get; private set; } = null!;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<JwtValidator>>();
            clientService = new Mock<IClientService>();

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            JwtValidator = new JwtValidator(logger.Object, configuration, clientService.Object);
        }

        [TestCase("jwt_validator_invalid_token", "foo", "", false, "ERR-JWT-004: request - token validation error", ErrorCodes.Generic.InvalidClient)]
        public async Task Validate_Jwt_Validator_InvalidClient_Test(
            string testCaseType,
            string client_id,
            string jwt,
            bool isvalid,
            string expectErrorDescription,
            string expectedError)
        {
            // Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            List<string>? audiences = null;
            List<string>? validAlgos = null;
            if (string.Equals("jwt_validator_invalid_token", testCaseType))
            {
                jwt = GetJwtToken();
                GetClient(client_id);
                audiences = ["https://localhost:8081", "https://localhost:8082/connect/token"];
                validAlgos = [Algorithms.Signing.PS256, Algorithms.Signing.ES256];
                ValidationResult validationResult = new ValidationResult(isvalid);
                mockJwtValidatorResult = (validationResult, GetJwt());
            }

            // Act
            var result = await JwtValidator.Validate(jwt, client, JwtValidationContext.Request, configOptions, audiences, validAlgos);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.ValidationResult.IsValid, isvalid);
            Assert.AreEqual(result.ValidationResult.Error, expectedError);
            Assert.IsTrue(result.ValidationResult.ErrorDescription?.Contains(expectErrorDescription));
        }

        private void GetClient(string client_id)
        {
            client = new Client
            {
                ClientId = client_id,
            };
            clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);
        }
    }
}
