using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.UnitTests.Validators
{
    public class TokenRequestValidatorTest : BaseTest
    {
        private Mock<ILogger<TokenRequestValidator>> logger = null!;
        private Mock<IClientService> clientService = null!;
        private Mock<IGrantService> grantService = null!;
        private Mock<ITokenService> tokenService = null!;
        private Mock<ICdrService> cdrService = null!;

        private IConfiguration configuration = null!;

        private TokenRequestValidator TokenRequestValidator = null!;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<TokenRequestValidator>>();

            clientService = new Mock<IClientService>();
            grantService = new Mock<IGrantService>();
            tokenService = new Mock<ITokenService>();
            cdrService = new Mock<ICdrService>();

            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.UnitTest.json")
                 .AddEnvironmentVariables()
                 .Build();

            TokenRequestValidator = new TokenRequestValidator(grantService.Object, tokenService.Object, clientService.Object, cdrService.Object);
        }

        [TestCase("token_client_id_missing", "", "", "", "", "", false, "client_id is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_is_missing", "foo", "", "", "", "", false, "invalid token request", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_granttype_missing", "foo", "", "", "", "", false, "grant_type is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_granttype_missing", "foo", "foo", "", "", "", false, "unsupported grant_type", ErrorCodes.Generic.UnsupportedGrantType)]
        [TestCase("token_granttype_supported", "foo", "refresh_token", "", "", "", false, "Could not retrieve client metadata", ErrorCodes.Generic.InvalidRequest)] // authorization_code refresh_token client_credentials
        [TestCase("token_client_id_unmatched", "foo", "refresh_token", "", "", "", false, "client_id does not match", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_software_product_id_empty", "foo", "refresh_token", "", "", "", false, "Could not retrieve client metadata", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_software_product_id_foo", "foo", "refresh_token", "foo", "", "", false, "Software product not found", ErrorCodes.Generic.InvalidClient)]
        [TestCase("token_software_product_inactive", "foo", "refresh_token", "foo", "INACTIVE", "", false, "Software product status is INACTIVE", "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive")]
        [TestCase("token_request_code_missing", "foo", "authorization_code", "foo", "ACTIVE", "", false, "code is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_redirect_uri_missing", "foo", "authorization_code", "foo", "ACTIVE", "foo", false, "redirect_uri is missing", ErrorCodes.Generic.InvalidRequest)]
        [TestCase("token_code_verifier_missing", "foo", "authorization_code", "foo", "ACTIVE", "foo", false, "code_verifier is missing", ErrorCodes.Generic.InvalidGrant)]
        [TestCase("token_code_verifier_foo", "foo", "authorization_code", "foo", "ACTIVE", "foo", false, "authorization code is invalid", ErrorCodes.Generic.InvalidGrant)]
        [TestCase("token_grant_expired", "foo", "authorization_code", "foo", "ACTIVE", "foo", false, "authorization code has expired", ErrorCodes.Generic.InvalidGrant)]
        [TestCase("token_refresh_token_missing", "foo", "refresh_token", "foo", "ACTIVE", "", false, "refresh_token is missing", ErrorCodes.Generic.InvalidGrant)]
        public async Task Validate_Token_Request_InvalidClient_Test(
            string testCaseType,
            string client_id,
            string grant_type,
            string softwareProductId,
            string brandStatus,
            string code,
            bool isvalid,
            string expectErrorDescription,
            string expectedError)
        {
            // Arrange
            var configOptions = this.configuration.GetConfigurationOptions();
            TokenRequest? tokenRequest = new()
            {
                Grant_type = grant_type,
            };

            if (string.Equals("token_is_missing", testCaseType))
            {
                tokenRequest = null;
            }

            if (string.Equals("token_client_id_unmatched", testCaseType))
            {
                tokenRequest!.Client_id = "unknown";
            }

            if (string.Equals("token_software_product_id_empty", testCaseType) || string.Equals("token_software_product_id_foo", testCaseType))
            {
                GetClient(client_id, softwareProductId);
            }

            if (string.Equals("token_software_product_inactive", testCaseType))
            {
                _ = new SoftwareProduct();
                GetSoftwareProduct(softwareProductId, brandStatus);
                GetClient(client_id, softwareProductId);
            }

            if (string.Equals("token_request_code_missing", testCaseType) || string.Equals("token_redirect_uri_missing", testCaseType))
            {
                tokenRequest!.Code = code;

                GetClient(client_id, softwareProductId);
                GetSoftwareProduct(softwareProductId, brandStatus);
            }

            if (string.Equals("token_code_verifier_missing", testCaseType))
            {
                tokenRequest!.Code = code;
                tokenRequest.Redirect_uri = "foo";
                tokenRequest.Code_verifier = string.Empty;

                GetClient(client_id, softwareProductId);
                GetSoftwareProduct(softwareProductId, brandStatus);
            }

            if (string.Equals("token_code_verifier_foo", testCaseType))
            {
                tokenRequest!.Code = code;
                tokenRequest.Redirect_uri = "foo";
                tokenRequest.Code_verifier = "foo";

                GetClient(client_id, softwareProductId);
                GetSoftwareProduct(softwareProductId, brandStatus);
            }

            if (string.Equals("token_grant_expired", testCaseType))
            {
                tokenRequest!.Code = code;
                tokenRequest.Redirect_uri = "foo";
                tokenRequest.Code_verifier = "foo";

                GetClient(client_id, softwareProductId);
                GetSoftwareProduct(softwareProductId, brandStatus);

                var grant = new AuthorizationCodeGrant();
                grant.ExpiresAt = DateTime.UtcNow.AddMinutes(-10);
                grantService.Setup(x => x.Get(GrantTypes.AuthCode, tokenRequest.Code, client_id)).ReturnsAsync(grant);
            }

            if (string.Equals("token_refresh_token_missing", testCaseType))
            {
                _ = new SoftwareProduct
                {
                    SoftwareProductId = softwareProductId,
                };
                tokenRequest!.Refresh_token = code;

                GetClient(client_id, softwareProductId);
                GetSoftwareProduct(softwareProductId, brandStatus);
            }

            // Act
            var result = await TokenRequestValidator.Validate(client_id, tokenRequest!, configOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.IsValid, isvalid);
            Assert.AreEqual(result.Error, expectedError);
            Assert.IsTrue(result.ErrorDescription?.Contains(expectErrorDescription), "Expected: {0}, Actual: {1}", expectErrorDescription, result.ErrorDescription);
        }

        private void GetSoftwareProduct(string softwareProductId, string brandStatus)
        {
            var softwareProduct = new SoftwareProduct();
            softwareProduct.SoftwareProductId = softwareProductId;
            softwareProduct.Status = brandStatus;
            softwareProduct.BrandStatus = brandStatus;
            softwareProduct.LegalEntityStatus = brandStatus;
            cdrService.Setup(x => x.GetSoftwareProduct(softwareProduct.SoftwareProductId)).ReturnsAsync(softwareProduct);
        }

        private void GetClient(string client_id, string softwareProductId)
        {
            var client = new Client
            {
                SoftwareId = softwareProductId,
            };
            clientService.Setup(x => x.Get(client_id)).ReturnsAsync(client);
        }
    }
}
