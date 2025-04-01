using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using CdrAuthServer.Configuration;
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
    public class ParValidatorTest : BaseTest
    {
        private Mock<ILogger<ParValidator>> logger = null!;
        private Mock<IClientService> clientService = null!;

        private Mock<IJwtValidator> jwtValidator = null!;

        private IConfiguration configuration = null!;

        private ParValidator parValidator = null!;

        public Client client { get; private set; } = null!;

        private (ValidationResult, JwtSecurityToken) mockJwtValidatorResult;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<ParValidator>>();

            clientService = new Mock<IClientService>();
            jwtValidator = new Mock<IJwtValidator>();

            var grantService = new Mock<IGrantService>();
            var requestObjectValidatorLogger = new Mock<ILogger<RequestObjectValidator>>();
            var requestObjectValidator = new RequestObjectValidator(requestObjectValidatorLogger.Object, clientService.Object, grantService.Object);

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            parValidator = new ParValidator(logger.Object, jwtValidator.Object, requestObjectValidator, clientService.Object);
        }

        [TestCase("par_request_client_id_missing", "", "", false, "request is not a well-formed JWT", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("par_request_client_invalid_jwt", "foo", "", false, "", ErrorCodes.Generic.InvalidRequestObject)]
        [TestCase("par_request_client_jwt_validator", "foo", "", false, "", ErrorCodes.Generic.InvalidRequestObject)]
        public async Task Validate_Par_Request_InvalidClient_Test(
            string testCaseType,
            string client_id,
            string requestObject,
            bool isvalid,
            string expectErrorDescription,
            string expectedError)
        {
            // Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            if (string.Equals("par_request_client_invalid_jwt", testCaseType))
            {
                requestObject = GetJwtToken();
                GetClient(client_id);
            }

            if (string.Equals("par_request_client_jwt_validator", testCaseType))
            {
                requestObject = GetJwtToken();
                GetClient(client_id);

                var audiences = new List<string>() { "https://localhost:8081", "https://localhost:8082/connect/token" };
                var validAlgos = new List<string>() { "PS256", "ES256" };
                ValidationResult validationResult = new(isvalid);

                mockJwtValidatorResult = (validationResult, GetJwt());
                jwtValidator.Setup(x => x.Validate(requestObject, client, JwtValidationContext.Request, configOptions, audiences, validAlgos)).ReturnsAsync(mockJwtValidatorResult);
            }

            // Act
            var result = await parValidator.Validate(client_id, requestObject, configOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.ValidationResult.IsValid, isvalid);
            Assert.AreEqual(result.ValidationResult.Error, expectedError);
            Assert.IsTrue(result.ValidationResult.ErrorDescription?.Contains(expectErrorDescription));
        }

        [TestCase("valid_claims_json", "{\"sharing_duration\":30000,\"id_token\":{\"acr\":{\"essential\":true,\"values\":[\"urn:cds.au:cdr:3\"]}}}", true)]
        [TestCase("invalid_claims_json_malformed", "{\"sharing_duration\":30000,\"id_token\":{\"acr\":{\"essential\":true,\"values\":[\"urn:cds.au:cdr:3\"]}}", false)]
        [TestCase("invalid_claims_missing_id_token", "{\"sharing_duration\":30000}", false)]
        [TestCase("invalid_claims_missing_id_token_acr", "{\"sharing_duration\":30000,\"id_token\":{}}", false)]
        [TestCase("invalid_claims_missing_response_mode", "{\"sharing_duration\":30000,\"id_token\":{\"acr\":{\"essential\":true,\"values\":[\"urn:cds.au:cdr:3\"]}}}", false, "")]
        public async Task Validate_Par_Request_ValidateClaimsJsonString_Test(string testCaseType, string claimsString, bool isValid, string responseMode = ResponseModes.Jwt)
        {
            // Arrange
            var requestObject = GetJwtToken(); // This just needs to be in correct format and the data inside doesn't matter.
            var configOptions = this.configuration.GetConfigurationOptions();
            var clienId = "client_id";
            var responseType = "code";
            var redirectUri = "https://redirect.uri";
            GetClient(clienId);
            client.RedirectUris = [redirectUri];
            client.ResponseTypes = [responseType];

            // Add additional claims that are required for the validations.
            Claim[] claims =
            [
                new Claim(ClaimNames.ResponseType, responseType),
                new Claim(ClaimNames.ResponseMode, responseMode),
                new Claim(ClaimNames.CodeChallengeMethod, "S256"),
                new Claim(ClaimNames.CodeChallenge, "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"), // Any string with length to be between 43 - 128
                new Claim(ClaimNames.Scope, "openid"), // Minimum required scope
                new Claim(ClaimNames.Nonce, "nonce"),
                new Claim(ClaimNames.Claims, claimsString),
            ];
            ValidationResult validationResult = new(true);
            mockJwtValidatorResult = (validationResult, GetJwt(clienId, redirectUri, true, claims));

            // Mock supporting methods
            jwtValidator
                .Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<Client>(), It.IsAny<JwtValidationContext>(), It.IsAny<ConfigurationOptions>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>()))
                .ReturnsAsync(mockJwtValidatorResult);
            clientService.Setup(x => x.Get(It.IsAny<string?>())).ReturnsAsync(client);

            // Act
            var result = await parValidator.Validate(clienId, requestObject, configOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.ValidationResult.IsValid, isValid);
        }

        private void GetClient(string client_id)
        {
            client = new Client();
            client.ClientId = client_id;
            clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);
        }
    }
}
