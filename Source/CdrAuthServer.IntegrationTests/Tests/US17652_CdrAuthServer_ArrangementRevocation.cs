#undef FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS // FIXME - MJS - the test using this code needs to be in MDH integration tests since it uses MDH endpoint (ie $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts")

using CdrAuthServer.IntegrationTests.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;
using static ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services.DataHolderAuthoriseService;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US17652_CdrAuthServer_ArrangementRevocation : BaseTest, IClassFixture<BaseFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderCDRArrangementRevocationService _cdrArrangementRevocationService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US17652_CdrAuthServer_ArrangementRevocation(IOptions<TestAutomationOptions> options,
            IOptions<TestAutomationAuthServerOptions> authServerOptions,
            IDataHolderRegisterService dataHolderRegisterService,
            IDataHolderParService dataHolderParService,
            IDataHolderTokenService dataHolderTokenService,
            ISqlQueryService sqlQueryService,
            IDataHolderCDRArrangementRevocationService cdrArrangementRevocationService,
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
            _dataHolderRegisterService = dataHolderRegisterService ?? throw new ArgumentNullException(nameof(dataHolderRegisterService));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _cdrArrangementRevocationService = cdrArrangementRevocationService ?? throw new ArgumentNullException(nameof(cdrArrangementRevocationService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        private async Task Arrange()
        {
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            await _dataHolderRegisterService.RegisterSoftwareProduct();
        }

        int CountPersistedGrant(string persistedGrantType, string? key = null)
        {
            using var connection = new SqlConnection(_options.AUTHSERVER_CONNECTIONSTRING);
            connection.Open();

            SqlCommand selectCommand;
            if (key != null)
            {
                selectCommand = new SqlCommand($"select count(*) from grants where granttype=@type and [key]=@key", connection);
                selectCommand.Parameters.AddWithValue("@key", key);
            }
            else
            {
                selectCommand = new SqlCommand($"select count(*) from grants where granttype=@type", connection);
            }
            selectCommand.Parameters.AddWithValue("@type", persistedGrantType);

            return selectCommand.ExecuteScalarInt32();
        }

        [Fact]
        // When an arrangement exists, revoking the arrangement should remove the arrangement and revoke any associated tokens
        public async Task AC01_Post_WithArrangementId_ShouldRespondWith_204NoContent_ArrangementRevoked()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode
            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
               .WithUserId(Constants.Users.UserIdKamillaSmith)
               .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
               .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
               .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get token and cdrArrangementId
            var cdrArrangementId = (await _dataHolderTokenService.GetResponse(authCode))?.CdrArrangementId;

            // Act - Revoke CDR arrangement
            var response = await _cdrArrangementRevocationService.SendRequest(cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                await Assertions.AssertHasNoContent2(response.Content);

                // Assert - Check persisted grants no longer exist
                CountPersistedGrant("refresh_token").Should().Be(0);
                CountPersistedGrant("cdr_arrangement_grant", cdrArrangementId).Should().Be(0);
            }
        }

        async Task RevokeCdrArrangement(string cdrArrangementId)
        {
            var response = await _cdrArrangementRevocationService.SendRequest(cdrArrangementId: cdrArrangementId);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new Exception("Error revoking cdr arrangement");
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // When an arrangement has been revoked, trying to use associated access token should result in error (Unauthorised)
        public async Task AC02_GetAccounts_WithRevokedAccessToken_ShouldRespondWith_401Unauthorised(bool revokeArrangement, HttpStatusCode expectedStatusCode)
        {
#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
            static async Task<HttpResponseMessage> GetAccounts(string? accessToken)
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

                return response;
            }
#endif

            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(revokeArrangement), revokeArrangement, nameof(expectedStatusCode), expectedStatusCode);

            // Arrange
            await Arrange();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
           .WithUserId(Constants.Users.UserIdKamillaSmith)
           .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
           .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
           .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);
            if (tokenResponse == null || tokenResponse.AccessToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Arrange - Revoke the arrangement
            if (revokeArrangement)
            {
                await RevokeCdrArrangement(tokenResponse.CdrArrangementId);
            }

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
            // Act - Use access token to get accounts. The access token should have been revoked because the arrangement was revoked
            var response = await GetAccounts(tokenResponse.AccessToken);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    await Assertions.Assert_HasNoContent2(response.Content);
                }
            }
#endif            
        }

        [Fact]
        public async Task AC03_GetAccessToken_WithValidRefreshToken_Success()
        {
            // Arrange
            await Arrange();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
           .WithUserId(Constants.Users.UserIdKamillaSmith)
           .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
           .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
           .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);//, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
            if (tokenResponse == null || tokenResponse.RefreshToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Act - Use refresh token to get a new access token. The refresh token should have been revoked because the arrangement was revoked            
            var response = await _dataHolderTokenService.SendRequest(
                grantType: "refresh_token",
                refreshToken: tokenResponse?.RefreshToken,
                scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS
                );

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        // When an arrangement has been revoked, trying to use associated refresh token to get newly minted access token should result in error (401Unauthorised)
        public async Task AC03_GetAccessToken_WithRevokedRefreshToken_ShouldRespondWith_401Unauthorised()
        {
            // Arrange
            await Arrange();

            var expectedError = new InvalidRefreshTokenException();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
           .WithUserId(Constants.Users.UserIdKamillaSmith)
           .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
           .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
           .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);//, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
            if (tokenResponse == null || tokenResponse.RefreshToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Arrange - Revoke the arrangement
            await RevokeCdrArrangement(tokenResponse.CdrArrangementId);

            // Act - Use refresh token to get a new access token. The refresh token should have been revoked because the arrangement was revoked            
            var response = await _dataHolderTokenService.SendRequest(
                grantType: "refresh_token",
                refreshToken: tokenResponse?.RefreshToken,
                scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS
                );

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid arrangementid should result in error (422UnprocessableEntity)
        public async Task AC04_POST_WithInvalidArrangementID_ShouldRespondWith_422UnprocessableEntity()
        {
            // Arrange
            await Arrange();

            // Act
            var cdrArrangementId = "foo";
            var response = await _cdrArrangementRevocationService.SendRequest(cdrArrangementId: cdrArrangementId);

            var expectedError = new InvalidArrangementException(cdrArrangementId);
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedError.StatusCode);

                await Assertions.AssertHasContentJson(expectedContent, response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with arrangementid that is not associated with the DataRecipient should result in error (422UnprocessableEntity)
        public async Task AC05_POST_WithNonAssociatedArrangementID_ShouldRespondWith_422UnprocessableEntity()
        {
            async Task<string> ArrangeAdditionalDataRecipient()
            {
                // Patch Register for additional data recipient
                Helpers.AuthServer.PatchRedirectUriForRegister(_options,
                    Constants.SoftwareProducts.AdditionalSoftwareProductId,
                    _options.ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
                Helpers.AuthServer.PatchJwksUriForRegister(_options,
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

            // Arrange
            await Arrange();
            string originalRegisteredClientId = _options.LastRegisteredClientId;

            // Arrange - Get authcode and thus create a CDR arrangement for ADDITIONAL_SOFTWAREPRODUCT_ID client
            await ArrangeAdditionalDataRecipient();
            string additionalClientId = _options.LastRegisteredClientId;

            var requestUri = await _dataHolderParService.GetRequestUri(
                scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                clientId: additionalClientId,
                jwtCertificateForClientAssertionFilename: Constants.Certificates.AdditionalJwksCertificateFilename,
                jwtCertificateForClientAssertionPassword: Constants.Certificates.AdditionalJwksCertificatePassword,
                jwtCertificateForRequestObjectFilename: Constants.Certificates.AdditionalJwksCertificateFilename,
                jwtCertificateForRequestObjectPassword: Constants.Certificates.AdditionalJwksCertificatePassword,
                redirectUri: _options.ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                sharingDuration: Constants.AuthServer.SharingDuration);

            // Arrange - Get authcode
            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, true, _authServerOptions)
           .WithUserId(Constants.Users.UserIdKamillaSmith)
           .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
           .WithClientId(additionalClientId)
           .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
           .WithCertificateFilename(Constants.Certificates.CertificateFilename)
           .WithCertificatePassword(Constants.Certificates.CertificatePassword)
           .WithRequestUri(requestUri)
           .BuildAsync();

            (var additional_authCode, _) = await authService.Authorise();

            // Arrange - Get the cdrArrangementId created by ADDITIONAL_SOFTWAREPRODUCT_ID client
            var additional_cdrArrangementId = (await _dataHolderTokenService.GetResponse(
                additional_authCode,
                clientId: additionalClientId,
                redirectUri: _options.ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                jwkCertificateFilename: Constants.Certificates.AdditionalJwksCertificateFilename,
                jwkCertificatePassword: Constants.Certificates.AdditionalJwksCertificatePassword
            ))?.CdrArrangementId;

            var expectedError = new InvalidArrangementException(additional_cdrArrangementId);
            var expectedContent = JsonConvert.SerializeObject(new ResponseErrorListV2(expectedError));

            // Act - Have original registered client attempt to revoke CDR arrangement created by ADDITIONAL_SOFTWAREPRODUCT_ID client
            var response = await _cdrArrangementRevocationService.SendRequest(clientId: originalRegisteredClientId, cdrArrangementId: additional_cdrArrangementId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(expectedError.StatusCode);

                // Assert - Check json
                await Assertions.AssertHasContentJson(expectedContent, response.Content);

                // Assert - Check persisted grants still exist
                CountPersistedGrant("refresh_token").Should().Be(1);
                CountPersistedGrant("cdr_arrangement", additional_cdrArrangementId).Should().Be(1);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientid should result in error (401Unauthorised)
        public async Task AC07_POST_WithInvalidClientId_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            await Arrange();

            var expectedError = new ClientNotFoundException();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
            .WithUserId(Constants.Users.UserIdKamillaSmith)
            .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
            .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
            .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await _dataHolderTokenService.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await _cdrArrangementRevocationService.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientId: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientassertiontype should result in error (401Unauthorised)
        public async Task AC08a_POST_WithInvalidClientAssertionType_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            await Arrange();

            var expectedError = new InvalidClientAssertionTypeException();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
        .WithUserId(Constants.Users.UserIdKamillaSmith)
        .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
        .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
        .BuildAsync();

            (var authCode, _) = await authService.Authorise();


            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await _dataHolderTokenService.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await _cdrArrangementRevocationService.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientAssertionType: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientassertion should result in error (401Unauthorised)
        public async Task AC08b_POST_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            await Arrange();

            var expectedError = new InvalidClientAssertionFormatException();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
       .WithUserId(Constants.Users.UserIdKamillaSmith)
       .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
       .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
       .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await _dataHolderTokenService.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await _cdrArrangementRevocationService.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientAssertion: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC09_POST_WithCurrentHolderOfKey_Success_NoContent()
        {
            // Arrange
            await Arrange();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
       .WithUserId(Constants.Users.UserIdKamillaSmith)
       .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
       .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
       .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await _dataHolderTokenService.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await _cdrArrangementRevocationService.SendRequest(
                cdrArrangementId: cdrArrangementId,
                jwtCertificateFilename: Constants.Certificates.JwtCertificateFilename,
                jwtCertificatePassword: Constants.Certificates.JwtCertificatePassword);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.AdditionalCertificateFilename, Constants.Certificates.AdditionalCertificatePassword)]  // ie different holder of key
        // Calling revocation endpoint with different holder of key should result in error
        public async Task AC09_POST_WithDifferentHolderOfKey_ShouldRespondWith_400BadRequest(string jwtCertificateFilename, string jwtCertificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(jwtCertificateFilename), jwtCertificateFilename, nameof(jwtCertificatePassword), jwtCertificatePassword);

            // Arrange
            await Arrange();

            var expectedError = new TokenValidationClientAssertionException();

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
       .WithUserId(Constants.Users.UserIdKamillaSmith)
       .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
       .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
       .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await _dataHolderTokenService.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await _cdrArrangementRevocationService.SendRequest(
                cdrArrangementId: cdrArrangementId,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }
    }
}
