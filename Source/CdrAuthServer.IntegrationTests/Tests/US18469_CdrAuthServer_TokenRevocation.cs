#undef FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS // FIXME - MJS - the test using this code needs to be in MDH integration tests since it uses MDH endpoint (ie $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts")

using System.Net;
#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
using System.Net.Http;
#endif
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;
using Microsoft.Extensions.Options;
using static ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services.DataHolderAuthoriseService;
using Xunit.DependencyInjection;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using Microsoft.Extensions.Configuration;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using Serilog;

namespace CdrAuthServer.IntegrationTests
{
    public class US18469_CdrAuthServer_TokenRevocation : BaseTest, IClassFixture<BaseFixture>, IAsyncLifetime
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderTokenRevocationService _tokenRevocationService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US18469_CdrAuthServer_TokenRevocation(IOptions<TestAutomationOptions> options,
            IOptions<TestAutomationAuthServerOptions> authServerOptions,
            IDataHolderParService dataHolderParService,
            IDataHolderRegisterService dataHolderRegisterService,
            IDataHolderTokenService dataHolderTokenService,
            ISqlQueryService sqlQueryService,
            IDataHolderTokenRevocationService tokenRevocationService,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _dataHolderRegisterService = dataHolderRegisterService ?? throw new ArgumentNullException(nameof(dataHolderRegisterService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _tokenRevocationService = tokenRevocationService ?? throw new ArgumentNullException(nameof(tokenRevocationService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));

        }

        public async Task InitializeAsync()
        {
            // Purge Authorisation Server Registrations and create a clean registration for each test to ensure test independance.
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            await _dataHolderRegisterService.RegisterSoftwareProduct(responseType: "code,code id_token");
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        // Call authorise/token endpoints to create access and refresh tokens
        private async Task<(string accessToken, string refreshToken)> CreateTokens(string? clientId = null,
            int sharingDuration = Constants.AuthServer.SharingDuration)
        {
            if (clientId == null)
            {
                clientId = _options.LastRegisteredClientId;
            }

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
              .WithUserId(Constants.Users.UserIdKamillaSmith)
              .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
              .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
              .WithSharingDuration(sharingDuration)
              .WithClientId(clientId)
              .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode, clientId: clientId);
            if (tokenResponse == null || tokenResponse.AccessToken == null || tokenResponse.RefreshToken == null)
                throw new Exception("Error getting access/refresh tokens");

            return (tokenResponse.AccessToken, tokenResponse.RefreshToken);
        }

        private async Task<string> ArrangeAdditionalDataRecipient()
        {
            // Patch Register for additional data recipient
            Helpers.AuthServer.PatchRedirectUriForRegister(
                _options,
                Constants.SoftwareProducts.AdditionalSoftwareProductId,
                _options.ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);

            Helpers.AuthServer.PatchJwksUriForRegister(
                _options,
                Constants.SoftwareProducts.AdditionalSoftwareProductId,
                _options.ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS);

            // Stand-up JWKS endpoint for additional data recipient
            var jwks_endpoint = new JwksEndpoint(
                _options.ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS,
                Constants.Certificates.AdditionalJwksCertificateFilename,
                Constants.Certificates.AdditionalJwksCertificatePassword);
            jwks_endpoint.Start();

            // Register software product for additional data recipient
            var (_, _, clientId) = await _dataHolderRegisterService.RegisterSoftwareProduct(
                    Constants.Brands.AdditionalBrandId,
                    Constants.SoftwareProducts.AdditionalSoftwareProductId,
                    Constants.Certificates.AdditionalJwksCertificateFilename,
                    Constants.Certificates.AdditionalJwksCertificatePassword);
            return clientId;

        }

        [Fact]
        // Revoke an access token
        public async Task AC01_Post_WithAccessToken_ShouldRespondWith_200OK()
        {
            // Arrange
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(token: accessToken, tokenTypeHint: "access_token");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                await Assertions.AssertHasNoContent2(response.Content);
            }
        }

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
        /// <summary>
        /// Call resource api using the access token. 
        /// </summary>
        static private async Task<HttpStatusCode> CallResourceAPI(string accessToken)
        {
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            return response.StatusCode;
        }
#endif

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // Try to use a revoked access token to call a resource API
        public async Task AC02_CallResourceAPI_WithRevokedAccessToken_ShouldRespondWith_401Unauthorised(bool revoke, HttpStatusCode expectedResourceAPIStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(revoke), revoke, nameof(expectedResourceAPIStatusCode), expectedResourceAPIStatusCode);

            // Arrange
            (var accessToken, _) = await CreateTokens();

            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                if (revoke)
                {
                    // Act - Revoke access token
                    var response = await _tokenRevocationService.SendRequest(token: accessToken, tokenTypeHint: "access_token");

                    // Assert
                    response.StatusCode.Should().Be(HttpStatusCode.OK);

                    await Assertions.AssertHasNoContent2(response.Content);
                }

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                // Assert - Check call to resource API returns correct status code
                (await CallResourceAPI(accessToken)).Should().Be(expectedResourceAPIStatusCode);
#endif                
            }
        }

        [Fact]
        // Revoke a refresh token
        public async Task AC03_Post_WithRefreshToken_ShouldRespondWith_200OK()
        {
            // Arrange
            (_, var refreshToken) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(token: refreshToken, tokenTypeHint: "refresh_token");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                await Assertions.AssertHasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // Try to use a revoked refresh token to get new access and refresh token
        public async Task AC04_CallTokenAPI_WithRevokedRefreshToken_ShouldRespondWith_400BadRequest(bool revoke, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(revoke), revoke, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            (_, var refreshToken) = await CreateTokens();

            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                if (revoke)
                {
                    // Act - Revoke access token
                    var response = await _tokenRevocationService.SendRequest(token: refreshToken, tokenTypeHint: "refresh_token");

                    // Assert
                    response.StatusCode.Should().Be(HttpStatusCode.OK);

                    await Assertions.AssertHasNoContent2(response.Content);
                }

                // Assert - Requesting new tokens returns correct status code
                var responseMessage = await _dataHolderTokenService.SendRequest(grantType: "refresh_token", refreshToken: refreshToken);
                responseMessage.StatusCode.Should().Be(expectedStatusCode);
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke an invalid access token
        public async Task AC05_Post_WithInvalidAccessToken_ShouldRespondWith_200OK(string token, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(token), token, nameof(expectedStatusCode), expectedStatusCode);

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(token: token, tokenTypeHint: "access_token");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assertions.AssertHasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke an invalid refresh token
        public async Task AC06_Post_WithInvalidRefreshToken_ShouldRespondWith_200OK(string token, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(token), token, nameof(expectedStatusCode), expectedStatusCode);

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(token: token, tokenTypeHint: "refresh_token");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assertions.AssertHasNoContent2(response.Content);
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke access token with invalid token type hint
        public async Task AC07_Post_WithInvalidAccessTokenTypeHint_ShouldRespondWith_200OK(string tokenTypeHint, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenTypeHint), tokenTypeHint, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(token: accessToken, tokenTypeHint: tokenTypeHint);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assertions.AssertHasNoContent2(response.Content);

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                (await CallResourceAPI(accessToken)).Should().NotBe(HttpStatusCode.OK, "token should have been revoked");
#endif                
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.OK)]
        // Revoke refresh token with invalid token type hint
        public async Task AC08_Post_WithInvalidRefreshTokenTypeHint_ShouldRespondWith_200OK(string tokenTypeHint, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(tokenTypeHint), tokenTypeHint, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            (_, var refreshToken) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(token: refreshToken, tokenTypeHint: tokenTypeHint);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                await Assertions.AssertHasNoContent2(response.Content);

                // Assert - Requesting new tokens with revoked refresh token should fail
                (await _dataHolderTokenService.SendRequest(grantType: "refresh_token", refreshToken: refreshToken))
                    .StatusCode.Should().NotBe(HttpStatusCode.OK, "token should have been revoked");
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.AdditionalCertificateFilename, Constants.Certificates.AdditionalCertificatePassword, HttpStatusCode.OK)] // ie different holder of key
        // Revoke an access token with different holder of key
        public async Task AC09_Post_AccessTokenWithDifferentHolderOfKey_ShouldRespondWith_200OK(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // For this test we want to get an legitimate access token for a different client.
            // Then we want to authenticate successfully to the token revocation endpoint, but pass in the access
            // token that is not owned by the caller.
            // The token revocation should still return a 200 OK status, but the token should not have been revoked.

            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(jwtCertificateFilename), jwtCertificateFilename, nameof(jwtCertificatePassword), jwtCertificatePassword, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            string originalClientId = _sqlQueryService.GetClientId(Constants.SoftwareProducts.SoftwareProductId);

            string additionalClientId = await ArrangeAdditionalDataRecipient();
            (var accessToken, _) = await CreateTokens(originalClientId);

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientId: additionalClientId,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert status code
                response.StatusCode.Should().Be(expectedStatusCode);
                await Assertions.AssertHasNoContent2(response.Content);

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
#endif                
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.AdditionalCertificateFilename, Constants.Certificates.AdditionalCertificatePassword, HttpStatusCode.OK)] // ie different holder of key
        public async Task AC10_Post_RefreshTokenWithDifferentHolderOfKey_ShouldRespondWith_200OK(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // For this test we want to get an legitimate access token for a different client.
            // Then we want to authenticate successfully to the token revocation endpoint, but pass in the access
            // token that is not owned by the caller.
            // The token revocation should still return a 200 OK status, but the token should not have been revoked.

            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(jwtCertificateFilename), jwtCertificateFilename, nameof(jwtCertificatePassword), jwtCertificatePassword, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange            
            string originalClientId = _sqlQueryService.GetClientId(Constants.SoftwareProducts.SoftwareProductId);

            string additionalClientId = await ArrangeAdditionalDataRecipient();
            (_, var refreshToken) = await CreateTokens(originalClientId);

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(
                token: refreshToken,
                tokenTypeHint: "refresh_token",
                clientId: additionalClientId,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert status code
                response.StatusCode.Should().Be(expectedStatusCode);
                await Assertions.AssertHasNoContent2(response.Content);

                // Assert - Requesting new tokens with refresh token should not fail, since refresh token should not have been revoked
                (await _dataHolderTokenService.SendRequest(clientId: originalClientId, issuerClaim: originalClientId, grantType: "refresh_token", refreshToken: refreshToken))
                    .StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        // Revoke an access token with invalid client id
        public async Task AC12_Post_AccessTokenWithInvalidClientId_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new ClientNotFoundException();
            string originalClientId = _sqlQueryService.GetClientId(Constants.SoftwareProducts.SoftwareProductId);

            (var accessToken, _) = await CreateTokens(originalClientId);

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientId: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
#endif                
            }
        }

        [Fact]
        // Revoke an access token with invalid client assertion type
        public async Task AC13_Post_AccessTokenWithInvalidClientAssertionType_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            var expectedError = new InvalidClientAssertionTypeException();

            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientAssertionType: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
#endif                
            }
        }

        [Fact]
        // Revoke an access token with invalid client assertion
        public async Task AC14_Post_AccessTokenWithInvalidClientAssertion_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            var expectedError = new InvalidClientAssertionFormatException();

            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                clientAssertion: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
#endif                
            }
        }

        [Fact]
        // Revoke an access token with client assertion signed with invalid certificate
        public async Task AC15_Post_AccessTokenWithClientAssertionSignedWithInvalidCert_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            var expectedError = new TokenValidationClientAssertionException();

            (var accessToken, _) = await CreateTokens();

            // Act - Revoke access token
            var response = await _tokenRevocationService.SendRequest(
                token: accessToken,
                tokenTypeHint: "access_token",
                jwtCertificateFilename: Constants.Certificates.AdditionalCertificateFilename, // ie this is not JWT_CERTIFICATE_FILENAME, hence it's not a valid certificate to sign JWT with
                jwtCertificatePassword: Constants.Certificates.AdditionalCertificatePassword
                );

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
                // Assert - Should be able to access resource API since token not revoked
                (await CallResourceAPI(accessToken)).Should().Be(HttpStatusCode.OK, "token should NOT have been revoked");
#endif                
            }
        }

    }
}
