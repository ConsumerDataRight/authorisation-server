using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;
using HttpMethod = System.Net.Http.HttpMethod;

namespace CdrAuthServer.IntegrationTests
{
    public class US12965_CdrAuthServer_UserInfo : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US12965_CdrAuthServer_UserInfo(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, ISqlQueryService sqlQueryService, IDataHolderAccessTokenCache dataHolderAccessTokenCache, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config, IApiServiceDirector apiServiceDirector)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        private class AC01_AC02_Expected
        {
#pragma warning disable IDE1006
            public string? given_name { get; set; }

            public string? family_name { get; set; }

            public string? name { get; set; }

            public string? iss { get; set; }

            public string? aud { get; set; }
#pragma warning restore IDE1006
        }

        private async Task Test_AC01_AC02(HttpMethod httpMethod, TokenType tokenType, string expectedName, string expectedGivenName, string expectedFamilyName)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);
            var expectedClientId = _options.LastRegisteredClientId;

            // Act
            var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, httpMethod);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<AC01_AC02_Expected>(actualJson);

                actual.Should().NotBeNull();
                actual?.name.Should().Be(expectedName);
                actual?.given_name.Should().Be(expectedGivenName);
                actual?.family_name.Should().Be(expectedFamilyName);
                actual?.iss.Should().Be(_options.DH_TLS_AUTHSERVER_BASE_URL);
                actual?.aud.Should().Be(expectedClientId);
            }
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, "Kamilla Smith", "Kamilla", "Smith")]
        public async Task AC01_Get_ShouldRespondWith_200OK_UserInfo(TokenType tokenType, string expectedName, string expectedGivenName, string expectedFamilyName)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedName), expectedName, nameof(expectedGivenName), expectedGivenName, nameof(expectedFamilyName), expectedFamilyName);

            await Test_AC01_AC02(HttpMethod.Get, tokenType, expectedName, expectedGivenName, expectedFamilyName);
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, "Kamilla Smith", "Kamilla", "Smith")]
        public async Task AC02_Post_ShouldRespondWith_200OK_UserInfo(TokenType tokenType, string expectedName, string expectedGivenName, string expectedFamilyName)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedName), expectedName, nameof(expectedGivenName), expectedGivenName, nameof(expectedFamilyName), expectedFamilyName);

            await Test_AC01_AC02(HttpMethod.Post, tokenType, expectedName, expectedGivenName, expectedFamilyName);
        }

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        [InlineData("SUSPENDED", "Suspended", HttpStatusCode.Forbidden)]
        [InlineData("REVOKED", "Revoked", HttpStatusCode.Forbidden)]
        [InlineData("SURRENDERED", "Surrendered", HttpStatusCode.Forbidden)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        public async Task AC03_Get_WithADRParticipationNotActive_ShouldRespondWith_403Forbidden_ErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(status), status, nameof(statusDescription), statusDescription, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var saveStatus = _sqlQueryService.GetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId);
            _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, "ACTIVE");

            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, status);

            try
            {
                // Act
                var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, System.Net.Http.HttpMethod.Get);
                var response = await api.SendAsync();

                var error = new AdrStatusNotActiveException($"ERR-GEN-002: Software product status is {statusDescription.ToUpper()}");
                var errorList = new ResponseErrorListV2(error.Code, error.Title, error.Detail, null);
                var expectedContent = JsonConvert.SerializeObject(errorList);

                // Assert
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);

                    // Assert - Check error response
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        await Assertions.AssertHasContentJson<ResponseErrorListV2>(expectedContent, response.Content);
                    }
                }
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.LEGALENTITY, Constants.LegalEntities.LegalEntityId, saveStatus);
            }
        }

        [Theory]
        [InlineData("ACTIVE", "Active", HttpStatusCode.OK)]
        [InlineData("INACTIVE", "Inactive", HttpStatusCode.Forbidden)]
        [InlineData("REMOVED", "Removed", HttpStatusCode.Forbidden)]
        public async Task AC05_Get_WithADRSoftwareProductNotActive_ShouldRespondWith_403Forbidden_ErrorResponse(string status, string statusDescription, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(status), status, nameof(statusDescription), statusDescription, nameof(expectedStatusCode), expectedStatusCode);

            await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith); // Ensure token cache is populated before changing status in case InlineData scenarios above are run/debugged out of order

            var saveStatus = _sqlQueryService.GetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId);
            _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, status);
            try
            {
                // Arrange
                var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

                // Act
                var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, System.Net.Http.HttpMethod.Get);
                var response = await api.SendAsync();

                // Assert
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    // Assert - Check status code
                    response.StatusCode.Should().Be(expectedStatusCode);

                    // Assert - Check error response
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        var error = new AdrStatusNotActiveException($"ERR-GEN-002: Software product status is {statusDescription.ToUpper()}", string.Empty);
                        var errorList = new ResponseErrorListV2(error.Code, error.Title, error.Detail, null);

                        var expectedContent = JsonConvert.SerializeObject(errorList);

                        await Assertions.AssertHasContentJson(expectedContent, response.Content);
                    }
                }
            }
            finally
            {
                _sqlQueryService.SetStatus(EntityType.SOFTWAREPRODUCT, Constants.SoftwareProducts.SoftwareProductId, saveStatus);
            }
        }

        private async Task Test_AC06_AC07(TokenType tokenType, HttpStatusCode expectedStatusCode, string expectedWWWAuthenticateResponse)
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(tokenType);

            // Act
            var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, System.Net.Http.HttpMethod.Get);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await Assertions.AssertHasNoContent2(response.Content);
                }
            }
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, HttpStatusCode.OK)]
        [InlineData(TokenType.InvalidEmpty, HttpStatusCode.Unauthorized)]
        [InlineData(TokenType.InvalidOmit, HttpStatusCode.Unauthorized)]
        public async Task AC06_Get_WithNoAccessToken_ShouldRespondWith_401Unauthorised_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode);

            await Test_AC06_AC07(tokenType, expectedStatusCode, "Bearer");
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith, HttpStatusCode.OK)]
        [InlineData(TokenType.InvalidFoo, HttpStatusCode.Unauthorized)]
        public async Task AC07_Get_WithInvalidAccessToken_ShouldRespondWith_401Unauthorised_WWWAuthenticateHeader(TokenType tokenType, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenType), tokenType, nameof(expectedStatusCode), expectedStatusCode);

            await Test_AC06_AC07(tokenType, expectedStatusCode, @"Bearer error=""invalid_token""");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task AC08_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorised_WWWAuthenticateHeader(HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            var accessToken = Constants.AccessTokens.ConsumerAccessTokenBankingExpired;

            // Act
            var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, System.Net.Http.HttpMethod.Get);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Assertions.AssertHasHeader(
                        @"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        response.Headers,
                        "WWW-Authenticate",
                        true);
                }
            }
        }

        [Fact]
        public async Task AC09_Get_WithCurrentHolderOfKey_Success()
        {
            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            string? thumbprint = null;
            if (_authServerOptions.STANDALONE)
            {
                thumbprint = _authServerOptions.XTLSCLIENTCERTTHUMBPRINT;
            }

            // Act
            var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, thumbprint, HttpMethod.Get, Constants.Certificates.CertificateFilename, Constants.Certificates.CertificatePassword);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.AdditionalCertificateFilename, Constants.Certificates.AdditionalCertificatePassword)] // Different holder of key
        public async Task AC09_Get_WithDifferentHolderOfKey_ShouldRespondWith_401Unauthorised(string certificateFilename, string certificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(certificateFilename), certificateFilename, nameof(certificatePassword), certificatePassword);

            // Arrange
            var accessToken = await _dataHolderAccessTokenCache.GetAccessToken(TokenType.KamillaSmith);

            AuthoriseException expectedError = new InvalidKeyHolderException();

            string? thumbprint = null;
            if (_authServerOptions.STANDALONE)
            {
                thumbprint = certificateFilename == Constants.Certificates.AdditionalCertificateFilename ? _authServerOptions.XTLSADDITIONALCLIENTCERTTHUMBPRINT : _authServerOptions.XTLSCLIENTCERTTHUMBPRINT;
            }

            // Act
            var api = _apiServiceDirector.BuildUserInfoAPI("1", accessToken, thumbprint, HttpMethod.Get, certificateFilename, certificatePassword);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }
    }
}
