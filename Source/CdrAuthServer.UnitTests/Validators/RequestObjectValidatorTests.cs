using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
    public class RequestObjectValidatorTest : BaseTest
    {
        private Mock<ILogger<RequestObjectValidator>> logger = null!;
        private Mock<IClientService> clientService = null!;
        private Mock<IGrantService> grantService = null!;

        private IConfiguration configuration = null!;

        private RequestObjectValidator requestObjectValidator = null!;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<RequestObjectValidator>>();

            clientService = new Mock<IClientService>();
            grantService = new Mock<IGrantService>();

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            requestObjectValidator = new RequestObjectValidator(logger.Object, clientService.Object, grantService.Object);
        }

        [TestCase("missing_client_id", "foo", false, "client_id is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("jwt_test_case1", "foo", false, "client_id does not match client_id in request object JWT", ErrorCodes.Generic.UnauthorizedClient)]
        [TestCase("jwt_test_case2", "foo", false, "redirect_uri missing from request object JWT", ErrorCodes.Generic.InvalidRequestObject)]
        [TestCase("jwt_test_case3", "foo", false, "Invalid redirect_uri", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("jwt_test_case4", "foo", false, "Invalid redirect_uri for client", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("jwt_test_redirect_uri_match", "foo", false, "Invalid request - nbf is missing", ErrorCodes.Generic.InvalidRequestObject)]
        [TestCase("jwt_test_redirect_uri_match_with_nbf", "foo", false, "response_type is missing", ErrorCodes.Generic.InvalidRequest)]
        public async Task Validate_RequestObject_InvalidClient_Test(
            string testCaseType,
            string client_id,
            bool isvalid,
            string expectErrorDescription,
            string expectedError)
        {
            // Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            JwtSecurityToken requestObject = new JwtSecurityToken();

            if (string.Equals("jwt_test_case1", testCaseType))
            {
                requestObject = GetJwt(client_id: "unknown");
            }

            if (string.Equals("jwt_test_case2", testCaseType))
            {
                requestObject = GetJwt(client_id: client_id);
            }

            if (string.Equals("jwt_test_case3", testCaseType))
            {
                requestObject = GetJwt(client_id: client_id, RedirectUriValue: "foo/cburi");
            }

            if (string.Equals("jwt_test_case4", testCaseType))
            {
                requestObject = GetJwt(client_id, RedirectUriValue: "https://foo/cb");
            }

            if (string.Equals("jwt_test_redirect_uri_match", testCaseType))
            {
                var client = new Client();
                client.RedirectUris = new List<string>() { "https://foo/cb" }.AsEnumerable();
                requestObject = GetJwt(client_id, RedirectUriValue: "https://foo/cb");
                clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);
            }

            if (string.Equals("jwt_test_redirect_uri_match_with_nbf", testCaseType))
            {
                var client = new Client();
                client.RedirectUris = new List<string>() { "https://foo/cb" }.AsEnumerable();
                requestObject = GetJwt(client_id, RedirectUriValue: "https://foo/cb", isNbf: true);
                clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);
            }

            // Act
            var result = await requestObjectValidator.Validate(client_id, requestObject, configOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(isvalid, result.ValidationResult.IsValid);
            Assert.AreEqual(expectedError, result.ValidationResult.Error);
            Assert.IsTrue(result.ValidationResult.ErrorDescription?.Contains(expectErrorDescription));
        }
    }
}
