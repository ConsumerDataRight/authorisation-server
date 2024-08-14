using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using Dapper;
using FluentAssertions;
using FluentAssertions.Execution;
using Jose;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Xunit.DependencyInjection;
using static ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services.DataHolderAuthoriseService;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US12963_CdrAuthServer_Token : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        public const string SCOPE_TOKEN_ACCOUNTS = "openid bank:accounts.basic:read";
        public const string SCOPE_TOKEN_CUSTOMER = "openid common:customer.basic:read";
        public const string SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS = "openid bank:accounts.basic:read bank:transactions:read";
        public const string SCOPE_EXCEED = "openid bank:accounts.basic:read bank:transactions:read additional:scope";

        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IApiServiceDirector _apiServiceDirector;

        private string _clientId { get; set; }
        public US12963_CdrAuthServer_Token(IOptions<TestAutomationOptions> options,
            IOptions<TestAutomationAuthServerOptions> authServerOptions,
            IDataHolderParService dataHolderParService,
            IDataHolderTokenService dataHolderTokenService,
            ISqlQueryService sqlQueryService,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            Log.Information($"Constructor for {nameof(US12963_CdrAuthServer_Token)}");

            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        static void AssertAccessToken(string? accessToken)
        {
            accessToken.Should().NotBeNullOrEmpty();
            if (accessToken != null)
            {
                var decodedAccessToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "iss");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "sub");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "aud");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "exp");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "auth_time");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "cnf");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "client_id");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "software_id");
                decodedAccessToken.Claims.Should().Contain(c => c.Type == "scope");
            }
        }

        static void AssertIdToken(string? idToken)
        {
            string decryptedIdToken;

            if (idToken != null)
            {
                var handler = new JwtSecurityTokenHandler();

                // Only decrypt id token if it is encrypted (for hybrid flow scenarios)
                if (!string.IsNullOrEmpty(handler.ReadJwtToken(idToken).Header.Enc))
                {
                    // Decrypt the id token.
                    var privateKeyCertificate = new X509Certificate2(Constants.Certificates.JwtCertificateFilename, Constants.Certificates.JwtCertificatePassword, X509KeyStorageFlags.Exportable);
                    var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                    JweToken token = JWE.Decrypt(idToken, privateKey);
                    decryptedIdToken = token.Plaintext;
                }
                else
                {
                    decryptedIdToken = idToken;
                }

                var decodedIdToken = new JwtSecurityTokenHandler().ReadJwtToken(decryptedIdToken);
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "iss");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "sub");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "aud");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "exp");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "auth_time");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "acr");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "family_name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "given_name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "updated_at");
            }
            else
            {
                throw new Exception($"Cannot assert id token in {nameof(AssertIdToken)} method as id token is null.");
            }
        }

        #region TEST_SCENARIO_A_IDTOKEN_AND_ACCESSTOKEN        
        [Fact]
        public async Task AC01_Auth_Code_Flow_Post_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                    var tokenResponse = await _dataHolderTokenService.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Fact]
        public async Task AC01_Hybrid_Flow_Post_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken()
        {
            // Arrange
            var authCode = await GetAuthCode(responseType: ResponseType.CodeIdToken, responseMode: ResponseMode.Fragment);
            
            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                    var tokenResponse = await _dataHolderTokenService.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Fact]
        public async Task AC01_Post_WithoutClientId_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, clientId: Constants.Omit);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                    var tokenResponse = await _dataHolderTokenService.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Fact]
        public async Task AC02_Put_ShouldRespondWith_405MethodNotSupportedOr404NotFound()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, usePut: true);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Depending on what is being tested (gateway or direct to auth server service) a different
                // status code will be returned.  This is also true in the Sandbox and CDR environments, as
                // the error handling is performed by the gateway device.
                responseMessage.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
            }
        }

        [Fact]
        public async Task AC03_Post_WithValidRequest_GrantType_Success()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, grantType: "authorization_code");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("foo")]
        [InlineData(null)]
        public async Task AC03_Post_WithInvalidRequest_GrantType_ShouldRespondWith_400BadRequest_InvalidRequestErrorResponse(string grantType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(grantType), grantType);

            AuthoriseException expectedError;

            if (string.IsNullOrEmpty(grantType))
            {
                expectedError = new MissingGrantTypeException();
            }
            else
            {
                expectedError = new UnsupportedGrantTypeException();
            }

            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, grantType: grantType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }


        [Fact]
        public async Task AC03_Post_WithValidRequest_ClientId_Success()
        {
            // Arrange
            var requestClientId = _options.LastRegisteredClientId;

            var authCode = await GetAuthCode(); //note that this uses _clientId, so by specifying different values in requestClientId we ensure invalidClientId errors

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(
                authCode,
                clientId: requestClientId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task AC03_Post_WithMissingIssuer_ShouldRespondWith_400BadRequest_MissingIssClaimResponse()
        {

            // Arrange
            var authCode = await GetAuthCode(); //note that this uses _clientId, so by specifying different values in requestClientId we ensure invalidClientId errors

            AuthoriseException expectedError = new MissingIssClaimException();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(
                authCode,
                clientId: Constants.Omit,
                issuerClaim: Constants.Omit);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }


        [Fact]
        public async Task AC03_Post_WithInvalidClientAndIssuer_ShouldRespondWith_400BadRequest_ClientNotFoundResponse()
        {

            // Arrange
            var authCode = await GetAuthCode(); //note that this uses _clientId, so by specifying different values in requestClientId we ensure invalidClientId errors

            AuthoriseException expectedError = new ClientNotFoundException();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(
                authCode,
                clientId: "foo",
                issuerClaim: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC03_Post_WithValidRequest_ClientAssertionType_Success()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, clientAssertionType: Constants.ClientAssertionType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("foo")]
        [InlineData(null)]
        public async Task AC03_Post_WithInvalidRequest_ClientAssertionType_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(string clientAssertionType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientAssertionType), clientAssertionType);
            // Arrange
            var authCode = await GetAuthCode();

            AuthoriseException expectedError;
            if (string.IsNullOrEmpty(clientAssertionType))
            {
                expectedError = new MissingClientAssertionTypeException();
            }
            else
            {
                expectedError = new InvalidClientAssertionTypeException();
            }

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC03_Post_WithValidRequest_ClientAssertion_Success()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, useClientAssertion: true);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task AC03_Post_WithInvalidRequest_ClientAssertion_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse()
        {
            // Arrange
            var authCode = await GetAuthCode();
            var expectedError = new MissingClientAssertionException();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, useClientAssertion: false);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        public enum AC04_TestType { valid_jwt, omit_iss, omit_aud, omit_exp, omit_jti, exp_backdated }

        [Fact]
        public async Task AC04_Post_WithValidClientAssertion_Success()
        {
            // Arrange
            var authCode = await GetAuthCode();

            // Act
            var clientAssertion = GenerateClientAssertion(AC04_TestType.valid_jwt);
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, customClientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(AC04_TestType.omit_iss)]
        [InlineData(AC04_TestType.omit_aud)]
        [InlineData(AC04_TestType.omit_exp)]
        [InlineData(AC04_TestType.omit_jti)]
        [InlineData(AC04_TestType.exp_backdated)]
        public async Task AC04_Post_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(AC04_TestType testType)
        {
            Log.Information("Running test with Params: {P1}={V1}", nameof(testType), testType);

            // Arrange
            var authCode = await GetAuthCode();

            AuthoriseException expectedError = testType switch
            {
                AC04_TestType.omit_iss => new MissingIssClaimException(),
                AC04_TestType.omit_aud => new InvalidAudienceException(),
                AC04_TestType.omit_exp => new TokenValidationClientAssertionException(),
                AC04_TestType.omit_jti => new MissingJtiException(),
                AC04_TestType.exp_backdated => new ExpiredClientAssertionException(),
                _ => throw new NotSupportedException()
            };

            // Act
            var clientAssertion = GenerateClientAssertion(testType);
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, customClientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC05_Post_WithValidAuthCode_Success()
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options, true);

            var authCode = await GetAuthCode();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task AC05_Post_WithExpiredAuthCode_ShouldRespondWith_400BadRequest_InvalidGrant()
        {
            async Task ExpireAuthCode(string authCode)
            {
                using var connection = new SqlConnection(_options.AUTHSERVER_CONNECTIONSTRING);
                connection.Open();

                var count = await connection.ExecuteAsync("update grants set expiresat = @expiresAt where [key]=@key", new
                {
                    expiresAt = DateTime.UtcNow.AddDays(-90),
                    key = authCode
                });

                if (count != 1)
                {
                    throw new Exception($"No grant found for authcode '{authCode}'");
                }
            }

            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options, true);

            var authCode = await GetAuthCode();
            await ExpireAuthCode(authCode);

            var expectedError = new ExpiredAuthorizationCodeException();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC06_Post_WithMissingAuthCode_ShouldRespondWith_400BadRequest_InvalidGrant() //TODO: The title says should return InvalidGrant, but the test checks for InvalidRequest. Logged as Bug 63704
        {
            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode: null);
            var expectedError = new MissingAuthorizationCodeException();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC07_Post_WithInvalidAuthCode_ShouldRespondWith_400BadRequest_InvalidGrantResponse()
        {
            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode: "foo");
            var expectedError = new InvalidAuthorizationCodeException();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task ACXXX_Post_InvalidClientEncryptionKey_ShouldRespondWith_400BadRequest_InvalidXXX()
        {
            // Arrange
            var authCode = await GetAuthCode(responseType: ResponseType.CodeIdToken, responseMode: ResponseMode.Fragment);
            Helpers.AuthServer.UpdateAuthServerClientClaim(_options, _clientId, "id_token_encrypted_response_alg", "RSA-OAEP-256");

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode);

            // Assert
            var expectedError = new UnexpectedErrorException("Unable to get encryption key required for id_token encryption from client JWKS");
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        #endregion

        #region TEST_SCENARIO_B_IDTOKEN_ACCESSTOKEN_REFRESHTOKEN
        [Theory]
        [InlineData(3600)]
        public async Task AC08_Post_WithShareDuration_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken(int shareDuration)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(shareDuration), shareDuration);

            // Arrange
            var authCode = await GetAuthCode(100000);

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, shareDuration: shareDuration);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                    var tokenResponse = await _dataHolderTokenService.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertIdToken(tokenResponse?.IdToken);
                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }
        #endregion

        #region TEST_SCENARIO_C_USE_REFRESHTOKEN_FOR_NEW_ACCESSTOKEN
        private async Task<HttpResponseMessage> Test_AC09_AC10(string initalScope, string requestedScope)
        {
            async Task<(string? authCode, string? refreshToken)> GetRefreshToken(string scope)
            {
                // Create grant with specific scope
                var authCode = await GetAuthCode(100000, scope);

                var responseMessage = await _dataHolderTokenService.SendRequest(authCode, shareDuration: 3600);

                var tokenResponse = await _dataHolderTokenService.DeserializeResponse(responseMessage);
                if (tokenResponse?.RefreshToken == null)
                {
                    throw new Exception($"{nameof(AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken)}.{nameof(GetRefreshToken)} - Error getting refresh token");
                }

                // Just make sure refresh token was issued with correct scope
                if (tokenResponse?.Scope != scope)
                {
                    throw new Exception($"{nameof(AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken)}.{nameof(GetRefreshToken)} - Unexpected scope");
                }

                return (authCode, tokenResponse?.RefreshToken);
            }

            // Get a refresh token with initial scope
            var (authCode, refreshToken) = await GetRefreshToken(initalScope);

            // Use the refresh token to get a new accesstoken and new refreshtoken (with the requested scope)
            var responseMessage = await _dataHolderTokenService.SendRequest(
                grantType: "refresh_token",
                refreshToken: refreshToken,
                scope: requestedScope);

            return responseMessage;
        }

        [Fact]
        public async Task AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken()
        {
            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var tokenResponse = await _dataHolderTokenService.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();

                    AssertIdToken(tokenResponse?.IdToken);
                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Theory]
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS)] // Same scope - should be ok
        [InlineData(SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS, SCOPE_TOKEN_ACCOUNTS)] // Decreased in scope - should be ok
        public async Task AC10_Post_WithRefreshToken_AndValidScope_Success(string initialScope, string requestedScope)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(initialScope), initialScope, nameof(requestedScope), requestedScope);

            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(initialScope, requestedScope);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS)]
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_EXCEED)]
        public async Task AC10_Post_WithRefreshToken_AndInvalidScope_ShouldRespondWith_400BadRequest(string initialScope, string requestedScope)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(initialScope), initialScope, nameof(requestedScope), requestedScope);

            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(initialScope, requestedScope);
            var expectedError = new InvalidScopeException();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC11_Post_WithInvalidRefreshToken_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse()
        {
            // Arrange
            var authCode = await GetAuthCode();

            AuthoriseException expectedError = new InvalidRefreshTokenException();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, grantType: "refresh_token", refreshToken: "foo");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        [Fact]
        public async Task AC11_Post_WithMissingRefreshToken_ShouldRespondWith_400BadRequest_InvalidGrantErrorResponse()
        {
            // Arrange
            var authCode = await GetAuthCode();

            AuthoriseException expectedError = new MissingRefreshTokenException();

            // Act
            var responseMessage = await _dataHolderTokenService.SendRequest(authCode, grantType: "refresh_token", refreshToken: null);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }
        #endregion

        #region EXTRA_TESTS
        [Theory]
        [InlineData(null, false)]
        [InlineData(0, false)]
        [InlineData(10000, true)]
        public async Task ACX01_Authorise_WithSharingDuration_ShouldRespondWith_AccessTokenRefreshToken(int? sharingDuration, bool expectsRefreshToken)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(sharingDuration), sharingDuration, nameof(expectsRefreshToken), expectsRefreshToken);

            string authCode = await GetAuthCode(sharingDuration);
           
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS); // access token expiry
                tokenResponse?.CdrArrangementId.Should().NotBeNull();
                tokenResponse?.CdrArrangementId.Should().NotBeEmpty();

                if (expectsRefreshToken)
                {
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse?.AccessToken);

                }
                else
                {
                    tokenResponse?.RefreshToken.Should().BeNullOrEmpty();
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task ACX02_UseRefreshTokenMultipleTimes_ShouldRespondWith_AccessTokenRefreshToken(int usageAttempts)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(usageAttempts), usageAttempts);

            // Arrange - Get authcode, 
            var authCode = await GetAuthCode(100000);

            // Act - Get access token and refresh token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert 
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNull();
                tokenResponse?.RefreshToken.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS); // access token expiry

                var refreshToken = tokenResponse?.RefreshToken;
                for (int i = 1; i <= usageAttempts; i++)
                {
                    // Act - Use refresh token to get access token and refresh token
                    var refreshTokenResponse = await _dataHolderTokenService.GetResponseUsingRefreshToken(refreshToken);

                    // Assert
                    refreshTokenResponse.Should().NotBeNull();
                    refreshTokenResponse?.AccessToken.Should().NotBeNull();
                    refreshTokenResponse?.RefreshToken.Should().NotBeNull();
                    refreshTokenResponse?.RefreshToken.Should().Be(refreshToken); // same refresh token is returned 

                    refreshToken = refreshTokenResponse?.RefreshToken;
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ACX03_UseExpiredRefreshToken_ShouldRespondWith_400BadRequest(bool expired)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(expired), expired);

            const int SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN = 10;

            // Arrange - Get authcode
            var authCode = await GetAuthCode(SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN);

            // Act - Get access token and refresh token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert 
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNull();
                tokenResponse?.RefreshToken.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS); // access token expiry

                if (expired)
                {
                    // Assert - Check that refresh token will expire when we expect it to
                    var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse?.AccessToken);

                    // Arrange - wait until refresh token has expired
                    await Task.Delay((SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN + 10) * 1000);
                }

                // Act - Use refresh token to get access token and refresh token
                var refreshTokenResponseMessage = await _dataHolderTokenService.SendRequest(grantType: "refresh_token", refreshToken: tokenResponse?.RefreshToken);

                // Assert - If expired should be BadRequest otherwise OK
                refreshTokenResponseMessage.StatusCode.Should().Be(expired ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            }
        }

        private async Task<HttpStatusCode> CallResourceAPI(string accessToken)
        {
            var api = _apiServiceDirector.BuildCustomerResourceAPI(accessToken);
            var response = await api.SendAsync();

            return response.StatusCode;
        }

        private async Task<string> GetAuthCode(int? sharingDuration = Constants.AuthServer.SharingDuration, string? scope = null, ResponseType responseType = ResponseType.Code, ResponseMode responseMode = ResponseMode.Jwt)
        {
            if (_clientId == null)
            {
                _clientId = _options.LastRegisteredClientId;
            }

            if (scope.IsNullOrWhiteSpace())
            {
                scope = SCOPE_TOKEN_ACCOUNTS;
            }

            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
                 .WithUserId(Constants.Users.UserIdKamillaSmith)
                 .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
                 .WithScope(scope)
                 .WithClientId(_clientId)
                 .WithSharingDuration(sharingDuration)
                 .WithResponseMode(responseMode)
                 .WithResponseType(responseType)
                 .BuildAsync();
            (var authCode, _) = await authService.Authorise();

            return authCode;
        }
        #endregion

        private string GenerateClientAssertion(AC04_TestType testType)
        {
            string ISSUER = _options.LastRegisteredClientId;

            var now = DateTime.UtcNow;

            var additionalClaims = new List<Claim>
                {
                     new Claim("sub", ISSUER),
                     new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer)
                };

            if (testType != AC04_TestType.omit_iss)
            {
                additionalClaims.Add(new Claim("iss", "foo"));
            }

            string? aud = null;
            if (testType != AC04_TestType.omit_aud)
            {
                aud = $"{_options.DH_MTLS_GATEWAY_URL}/connect/token";
            }

            if (testType != AC04_TestType.omit_jti)
            {
                additionalClaims.Add(new Claim("jti", Guid.NewGuid().ToString()));
            }

            DateTime? expires = null;
            if (testType == AC04_TestType.exp_backdated)
            {
                expires = now.AddMinutes(-1);
            }
            else if (testType != AC04_TestType.omit_exp)
            {
                expires = now.AddMinutes(10);
            }

            var certificate = new X509Certificate2(Constants.Certificates.JwtCertificateFilename, Constants.Certificates.JwtCertificatePassword, X509KeyStorageFlags.Exportable);
            var x509SigningCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);

            var jwt = new JwtSecurityToken(
                (testType == AC04_TestType.omit_iss) ? null : ISSUER,
                aud,
                additionalClaims,
                expires: expires,
                signingCredentials: x509SigningCredentials);

            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

            return jwtSecurityTokenHandler.WriteToken(jwt);
        }
    }
}