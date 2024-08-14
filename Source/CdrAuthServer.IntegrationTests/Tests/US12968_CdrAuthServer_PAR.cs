using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.APIs;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Jose;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Xunit;
using Xunit.DependencyInjection;
using static ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services.DataHolderAuthoriseService;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US12968_CdrAuthServer_PAR : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US12968_CdrAuthServer_PAR(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IDataHolderParService dataHolderParService, ISqlQueryService sqlQueryService, IDataHolderTokenService dataHolderTokenService, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        [Fact]
        // Call PAR endpoint, with request, to get a RequestUri
        public async Task AC01_Post_ShouldRespondWith_201Created_RequestUri()
        {
            // Arrange

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE);

            //var responseText = await response.Content.ReadAsStringAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    var parResponse = await _dataHolderParService.DeserializeResponse(response);
                    parResponse.Should().NotBeNull();
                    parResponse?.RequestURI.Should().NotBeNullOrEmpty();
                    parResponse?.ExpiresIn.Should().Be(90);
                }
            }
        }

        [Theory]
        [InlineData("foo")] // Unknown ArrangementID
        public async Task AC06_Post_WithUnknownOrUnAssociatedCdrArrangementId_ShouldRespondWith_400BadRequest(string cdrArrangementId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(cdrArrangementId), cdrArrangementId);

            // Arrange
            var expectedError = new InvalidArrangementIdException();

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC08_Post_WithValidClientId_Success_Created()
        {
            var clientId = _options.LastRegisteredClientId;

            // Arrange

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, clientId: clientId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC08_Post_WithInvalidClientId_ShouldRespondWith_400BadRequest(string clientId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientId), clientId);

            // Arrange
            var expectedError = new ClientNotFoundException();

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, clientId: clientId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC09_Post_WithValidClientAssertionType_Success()
        {
            // Arrange

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, clientAssertionType: Constants.ClientAssertionType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC09_Post_WithInvalidClientAssertionType_ShouldRespondWith_400BadRequest(string clientAssertionType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientAssertionType), clientAssertionType);

            // Arrange
            var expectedErrror = new InvalidClientAssertionTypeException();
            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedErrror);
            }
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC10_Revocation_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest(string clientAssertion)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientAssertion), clientAssertion);

            // Arrange
            var expectedError = new InvalidClientAssertionFormatException();

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, clientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC11_Post_WitCurrentHolderOfKey_Success_Created()
        {
            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE,
                jwtCertificateForClientAssertionFilename: Constants.Certificates.JwtCertificateFilename,
                jwtCertificateForClientAssertionPassword: Constants.Certificates.JwtCertificatePassword);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        [Theory]
        [InlineData(Constants.Certificates.AdditionalCertificateFilename, Constants.Certificates.AdditionalCertificatePassword)] // ie different holder of key
        public async Task AC11_Post_WithDifferentHolderOfKey_ShouldRespondWith_400BadRequest(string jwtCertificateFilename, string jwtCertificatePassword)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(jwtCertificateFilename), jwtCertificateFilename, nameof(jwtCertificatePassword), jwtCertificatePassword);

            //Arrange
            var expectedError = new TokenValidationClientAssertionException();

            // Act
            var response = await _dataHolderParService.SendRequest(_options.SCOPE,
                jwtCertificateForClientAssertionFilename: jwtCertificateFilename,
                jwtCertificateForClientAssertionPassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, to create CdrArrangement
        public async Task AC02_Post_AuthorisationEndpoint_WithRequestUri_ShouldRespondWith_200OK_CdrArrangementId()
        {
            // Arrange
            var response = await _dataHolderParService.SendRequest(_options.SCOPE);
            if (response.StatusCode != HttpStatusCode.Created) throw new Exception("Error with PAR request - StatusCode");
            var parResponse = await _dataHolderParService.DeserializeResponse(response);
            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("Error with PAR request - RequestURI");
            if (parResponse?.ExpiresIn != 90) throw new Exception("Error with PAR request - ExpiresIn");


            // Act - Authorise with PAR RequestURI
            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
               .WithUserId(Constants.Users.UserIdKamillaSmith)
               .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
               .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
               .WithRequestUri(parResponse.RequestURI)
               .BuildAsync();

            (var authCode, var idToken) = await authService.Authorise();

            // Assert - Check we got an authCode and idToken
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                authCode.Should().NotBeNullOrEmpty();
            }

            // Act - Use the authCode to get token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);

            // Assert - Check we get back cdrArrangementId
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]

        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, but after requestUri has expired (90 seconds), should redirect to DH callback URI
        public async Task AC03_Post_AuthorisationEndpoint_WithRequestUri_After90Seconds_ShouldRespondWith_302Found_CallbackURI()
        {
            var expectedRedirectPath = _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
            var expectedError = new ExpiredRequestUriException();
         
            const int PAR_EXPIRY_SECONDS = 90;

            var clientId = _options.LastRegisteredClientId ?? throw new NullReferenceException("Could not find Client Id.");

            // Arrange
            var response = await _dataHolderParService.SendRequest(_options.SCOPE, clientId: clientId);
            if (response.StatusCode != HttpStatusCode.Created) throw new Exception("Error with PAR request - StatusCode");
            var parResponse = await _dataHolderParService.DeserializeResponse(response);
            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("Error with PAR request - RequestURI");
            if (parResponse?.ExpiresIn != PAR_EXPIRY_SECONDS) throw new Exception("Error with PAR request - ExpiresIn");

            // Wait until PAR expires
            await Task.Delay((PAR_EXPIRY_SECONDS + 10) * 1000);

            //May need to provide a clientId here!
            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                .WithRequestUri(parResponse.RequestURI)
                .WithClientId(clientId)
                .WithJWTCertificateFilename(Constants.Certificates.JwtCertificateFilename)
                .WithJWTCertificatePassword(Constants.Certificates.JwtCertificatePassword)
                .WithResponseType(ResponseType.Code.ToEnumMemberAttrValue())
            .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var authResponse = await Helpers.Web.CreateHttpClient(allowAutoRedirect: false).SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                authResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

                // Check redirect path
                var redirectPath = authResponse?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                var queryValues = HttpUtility.ParseQueryString(authResponse?.Headers.Location?.Query ?? throw new NullReferenceException());

                // Check query has "response" param
                var queryValueResponse = queryValues["response"];
                
                var encodedJwt = queryValueResponse;
                queryValueResponse.Should().NotBeNullOrEmpty();

                if (_authServerOptions.JARM_ENCRYPTION_ON)
                {
                    // Decrypt the JARM JWT.
                    var privateKeyCertificate = new X509Certificate2(Constants.Certificates.JwtCertificateFilename, Constants.Certificates.JwtCertificatePassword, X509KeyStorageFlags.Exportable);
                    var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                    JweToken token = JWE.Decrypt(queryValueResponse, privateKey);
                    encodedJwt = token.Plaintext;
                }

                // Check claims of decode jwt
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(encodedJwt);
                jwt.Claim("error").Value.Should().Be(expectedError.Error);
                jwt.Claim("error_description").Value.Should().Be(expectedError.ErrorDescription);

            }
        }

        [Fact]
        // Call PAR endpoint, with existing CdrArrangementId, to get requestUri
        public async Task AC04_Post_WithCdrArrangementId_ShouldRespondWith_201Created_RequestUri()
        {
            // Arrange 
            var cdrArrangementId = await CreateCDRArrangement();

            // Act - PAR with existing CdrArrangementId
            var response = await _dataHolderParService.SendRequest(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                var parResponse = await _dataHolderParService.DeserializeResponse(response);
                parResponse.Should().NotBeNull();
                parResponse?.RequestURI.Should().NotBeNullOrEmpty();
                parResponse?.ExpiresIn.Should().Be(90);
            }
        }

        [Fact]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, to update existing CdrArrangement
        public async Task AC05_Post_AuthorisationEndpoint_WithRequestUri_ShouldRespondWith_200OK_CdrArrangementId()
        {
            // Arrange
            // Create CDR arrangement, call PAR and get RequestURI
            var cdrArrangementId = await CreateCDRArrangement();

            //call PAR with existing CdrArrangementId and get RequestURI
            var response = await _dataHolderParService.SendRequest(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, cdrArrangementId: cdrArrangementId);
            var parResponse = await _dataHolderParService.DeserializeResponse(response);

            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("requestUri is null");

            // Act - Authorise using requestURI
            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
               .WithUserId(Constants.Users.UserIdKamillaSmith)
               .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
               .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
               .WithRequestUri(parResponse.RequestURI)
               .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Act - Use the authCode to get token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);

            // Assert - Check we get back cdrArrangementId
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                tokenResponse?.Scope.Should().Be(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
            }
        }

        [Fact]
        // Call PAR endpoint, with existing CdrArrangementId, to get requestUri
        public async Task AC04_Post_WithCdrArrangementId_AmendingConsent_ShouldInvalidateRefreshTokenn()
        {
            // Create a CDR arrangement
            async Task<(string cdrArrangementId, string refreshToken)> CreateCDRArrangement(string cdrArrangementId = null)
            {
                // Authorise
                var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
                    .WithUserId(Constants.Users.UserIdKamillaSmith)
                    .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
                    .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
                    .WithCdrArrangementId(cdrArrangementId)
                    .BuildAsync();

                (var authCode, _) = await authService.Authorise();

                // Get token
                var tokenResponse = await _dataHolderTokenService.GetResponse(authCode, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
                if (tokenResponse == null || tokenResponse?.CdrArrangementId == null) throw new Exception("Error getting CDRArrangementId");

                // Return CdrArrangementId
                return (tokenResponse.CdrArrangementId, tokenResponse.RefreshToken);
            }

            // Arrange 
            var (cdrArrangementId, refreshToken) = await CreateCDRArrangement();

            // Amend consent.
            var (newCrArrangementId, newRefreshToken) = await CreateCDRArrangement(cdrArrangementId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                Assert.Equal(cdrArrangementId, newCrArrangementId);
                Assert.NotEqual(refreshToken, newRefreshToken);
            }
        }

        private async Task<string> CreateCDRArrangement()
        {
            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
          .WithUserId(Constants.Users.UserIdKamillaSmith)
          .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
          .WithScope(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
          .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Get token
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
            if (tokenResponse == null || tokenResponse?.CdrArrangementId == null) throw new Exception("Error getting CDRArrangementId");

            // Return CdrArrangementId
            return tokenResponse.CdrArrangementId;
        }
    }
}
