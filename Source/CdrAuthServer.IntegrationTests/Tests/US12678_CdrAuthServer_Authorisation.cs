using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.APIs;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.CdsExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Jose;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Xunit;
using XUnit_Skippable;
using Xunit.DependencyInjection;
using static ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services.DataHolderAuthoriseService;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US12678_CdrAuthServer_Authorisation : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IApiServiceDirector _apiServiceDirector;
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;

        private readonly string _accountLoginUrl;

        public US12678_CdrAuthServer_Authorisation(
            IOptions<TestAutomationOptions> options,
            IOptions<TestAutomationAuthServerOptions> authServerOptions,
            IDataHolderParService dataHolderParService,
            IDataHolderTokenService dataHolderTokenService,
            ISqlQueryService sqlQueryService,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            Log.Information("Constructing {ClassName}.", nameof(US12678_CdrAuthServer_Authorisation));

            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions?.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));

            _accountLoginUrl = $"{_options.MDH_HOST}:8001/account/login";
        }

        private void Arrange()
        {
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options, true);
        }

        [Fact]
        public async Task AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken()
        {
            // Arrange
            Arrange();

            // Perform authorise and consent flow and get authCode
            var authService = await new DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
             .WithUserId(Constants.Users.UserIdKamillaSmith)
             .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
             .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // Act
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);
            if (tokenResponse == null)
            {
                throw new InvalidOperationException($"{nameof(AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken)} - TokenResponse is null");
            }

            if (tokenResponse.IdToken == null)
            {
                throw new InvalidOperationException($"{nameof(AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken)} - Id token is null");
            }

            if (tokenResponse.AccessToken == null)
            {
                throw new InvalidOperationException($"{nameof(AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken)} - Access token is null");
            }

            if (tokenResponse.RefreshToken == null)
            {
                throw new InvalidOperationException($"{nameof(AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken)} - Refresh token is null");
            }

            if (tokenResponse.CdrArrangementId == null)
            {
                throw new InvalidOperationException($"{nameof(AC01_Get_WithValidRequest_ShouldRespondWith_302Redirect_RedirectToRedirectURI_IdToken)} - CdrArrangementId is null");
            }

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse.ExpiresIn.Should().Be(_authServerOptions.ACCESSTOKENLIFETIMESECONDS);
                tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse.CdrArrangementId.Should().NotBeNullOrEmpty();
            }
        }

        [Theory]
        [InlineData(SoftwareProductStatus.INACTIVE, EntityType.SOFTWAREPRODUCT)]
        [InlineData(SoftwareProductStatus.REMOVED, EntityType.SOFTWAREPRODUCT)]

        public async Task AC02_InactiveOrRemovedSoftwareProduct_ShouldReturn_AdrStatusNotActiveException(SoftwareProductStatus status, EntityType entityType)
        {
            // Arrange
            Arrange();

            string entityId = Constants.SoftwareProducts.SoftwareProductId;

            var expectedError = new AdrStatusNotActiveException(status);

            var saveStatus = _sqlQueryService.GetStatus(entityType, entityId);
            _sqlQueryService.SetStatus(entityType, entityId, status.ToEnumMemberAttrValue());

            try
            {
                var response = await _dataHolderParService.SendRequest(_options.SCOPE, _options.LastRegisteredClientId);
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception("Error with PAR request - StatusCode");
                }

                var parResponse = await _dataHolderParService.DeserializeResponse(response);
                if (string.IsNullOrEmpty(parResponse?.RequestURI))
                {
                    throw new Exception("Error with PAR request - RequestURI");
                }

                var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                    .WithRequestUri(parResponse.RequestURI)
                    .WithClientId(_options.LastRegisteredClientId ?? throw new NullReferenceException("Client id cannot be null."))
                    .WithResponseType(ResponseType.Code.ToEnumMemberAttrValue())
                .Build().Url;

                var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

                var authResponse = await Helpers.Web.CreateHttpClient(allowAutoRedirect: false).SendAsync(request);

                // Check query has "response" param
                var queryValues = HttpUtility.ParseQueryString(authResponse?.Headers.Location?.Query ?? throw new NullReferenceException());
                var queryValueResponse = queryValues["response"];

                var encodedJwt = queryValueResponse;

                if (_authServerOptions.JARM_ENCRYPTION_ON)
                {
                    // Decrypt the JARM JWT.
                    var privateKeyCertificate = new X509Certificate2(
                        Constants.Certificates.JwtCertificateFilename,
                        Constants.Certificates.JwtCertificatePassword, X509KeyStorageFlags.Exportable);
                    var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                    JweToken token = JWE.Decrypt(queryValueResponse, privateKey);
                    encodedJwt = token.Plaintext;
                }

                // Check claims of decode jwt
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(encodedJwt);
                jwt.Claim("error").Value.Should().Be(expectedError.Code);
                jwt.Claim("error_description").Value.Should().Be(expectedError.Detail);
            }
            finally
            {
                _sqlQueryService.SetStatus(entityType, entityId, saveStatus);
            }
        }

        [SkippableTheory]
        [InlineData("code id_token", HttpStatusCode.Redirect, true)]
        [InlineData("foo", HttpStatusCode.Redirect, false)] // Unsuccessful request should redirect back to DR
        public async Task AC02_Get_WithInvalidResponseType_ShouldRespondWith_302Redirect_ErrorResponse(string responseType, HttpStatusCode expectedStatusCode, bool useSpecificRedirectUrl)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(expectedStatusCode), expectedStatusCode, nameof(useSpecificRedirectUrl), useSpecificRedirectUrl);

            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            string? expectedRedirectFragment = null;
            string expectedRedirectPath;
            if (useSpecificRedirectUrl)
            {
                expectedRedirectPath = _accountLoginUrl;
            }
            else
            {
                expectedRedirectPath = _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;

                var expectedError = new UnsupportedResponseTypeRedirectException();
                expectedRedirectFragment = GenerateErrorRedirectFragment(expectedError);
            }

            // Arrange
            Arrange();
            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                     .WithResponseType(responseType)
                     .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response.Headers.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response.Headers.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [SkippableTheory]
        [InlineData(null, HttpStatusCode.Redirect, true)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC03_Get_WithInvalidRequestBody_ShouldRespondWith_400BadRequest_ErrorResponse(string requestBody, HttpStatusCode expectedStatusCode, bool useSpecificUrl = false)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(requestBody), requestBody, nameof(expectedStatusCode), expectedStatusCode, nameof(useSpecificUrl), useSpecificUrl);

            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            var expectedRedirectPath = string.Empty;
            if (useSpecificUrl)
            {
                expectedRedirectPath = _accountLoginUrl;
            }

            // Arrange
            Arrange();

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                     .WithRequest(requestBody)
                     .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check redirect path
                if (expectedRedirectPath != null)
                {
                    // Check redirect path
                    var redirectPath = response.Headers.Location?.GetLeftPart(UriPartial.Path);
                    redirectPath.Should().Be(expectedRedirectPath);
                }
            }
        }

        [SkippableTheory]
        [InlineData(false, HttpStatusCode.Redirect)] // Successful request should redirect to the DH login URI
        [InlineData(
            true, // Additional unsupported scope should be ignored
            HttpStatusCode.Redirect)]
        public async Task AC04_Get_WithInvalidScope_ShouldRespondWith_200OK_Response(bool useAdditionalScope, HttpStatusCode expectedStatusCode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(useAdditionalScope), useAdditionalScope, nameof(expectedStatusCode), expectedStatusCode);

            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            var expectedRedirectPath = _accountLoginUrl;

            var scope = Constants.Scopes.ScopeBanking;
            if (useAdditionalScope)
            {
                scope += " admin:metadata:update";
            }

            // Arrange
            Arrange();

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                    .WithScope(scope)
                    .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            // Act
            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);
            }
        }

        [SkippableTheory]
        [InlineData(Constants.Scopes.ScopeBanking, HttpStatusCode.Redirect, false)]
        [InlineData(Constants.Scopes.ScopeBankingWithoutOpenId, HttpStatusCode.Redirect, true)]
        public async Task AC05_Get_WithScopeMissingOpenId_ShouldRespondWith_302Redirect_ErrorResponse(
            string scope,
            HttpStatusCode expectedStatusCode,
            bool useSpecificUrl)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(scope), scope, nameof(expectedStatusCode), expectedStatusCode, nameof(useSpecificUrl), useSpecificUrl);

            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            var expectedError = new MissingOpenIdScopeException();
            var errorRedirectFragment = GenerateErrorRedirectFragment(expectedError);

            string expectedRedirectPath;
            string? expectedRedirectFragment = null;
            if (useSpecificUrl)
            {
                expectedRedirectPath = _accountLoginUrl;
                expectedRedirectFragment = errorRedirectFragment;
            }
            else
            {
                expectedRedirectPath = $"{_options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS}{errorRedirectFragment}";
            }

            // Arrange
            Arrange();

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                   .WithScope(scope)
                   .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response.Headers.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response.Headers.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [SkippableTheory]
        [InlineData(Constants.SoftwareProducts.SoftwareProductId, HttpStatusCode.Redirect, true)]
        [InlineData(Constants.GuidFoo, HttpStatusCode.Redirect, false)] // Unsuccessful request should redirect back to DR
        public async Task AC06_Get_WithInvalidClientID_ShouldRespondWith_302Redirect_ErrorResponse(string softwareProductId, HttpStatusCode expectedStatusCode, bool useSpecificUrl)
        {
            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            string? expectedRedirectFragment = null;
            string expectedRedirectPath;
            if (useSpecificUrl)
            {
                expectedRedirectPath = _accountLoginUrl;
            }
            else
            {
                expectedRedirectPath = _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;

                var expectedError = new InvalidClientIdRedirectException();
                expectedRedirectFragment = GenerateErrorRedirectFragment(expectedError);
            }

            // Arrange
            Arrange();

            var clientId = _options.LastRegisteredClientId;

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                   .WithClientId(clientId)
                   .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response.Headers.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect fragment
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response.Headers.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [SkippableFact]
        public async Task AC07_Get_WithValidRedirectURI_Success()
        {
            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            var expectedRedirectPath = _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;

            // Arrange
            Arrange();

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                   .WithRedirectURI(expectedRedirectPath.ToLower())
                   .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            }
        }

        [SkippableFact]
        public async Task AC07_Get_WithInvalidRedirectURI_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            var expectedError = new InvalidRequestException(string.Empty); // TODO: Why doesn't this use a description? Bug 64158
            var expectedRedirectPath = "https://localhost:9001/foo";

            // Arrange
            Arrange();

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                   .WithRedirectURI(expectedRedirectPath.ToLower())
                   .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [SkippableTheory]
        [InlineData(Constants.Certificates.JwtCertificateFilename, Constants.Certificates.JwtCertificatePassword, HttpStatusCode.Redirect, true)]
        [InlineData(Constants.Certificates.InvalidCertificateFilename, Constants.Certificates.InvalidCertificatePassword, HttpStatusCode.Redirect, false)] // Unsuccessful request should redirect back to DR
        public async Task AC08_Get_WithUnsignedRequestBody_ShouldRespondWith_302Redirect_ErrorResponse(
            string jwt_certificateFilename,
            string jwt_certificatePassword,
            HttpStatusCode expectedStatusCode,
            bool useSpecificUrl)
        {
            if (_authServerOptions.HEADLESSMODE)
            {
                throw new SkipTestException("Test not applicable for headless mode.");
            }

            var expectedError = new InvalidJwtException();
            var errorRedirectFragment = GenerateErrorRedirectFragment(expectedError);

            string? expectedRedirectFragment = null;
            var expectedRedirectPath = _accountLoginUrl;
            if (!useSpecificUrl)
            {
                expectedRedirectFragment = errorRedirectFragment;
                expectedRedirectPath = _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
            }

            // Arrange
            Arrange();

            var authorisationURL = new AuthoriseUrl.AuthoriseUrlBuilder(_options)
                       .WithJWTCertificateFilename(jwt_certificateFilename)
                       .WithJWTCertificatePassword(jwt_certificatePassword)
                       .Build().Url;

            var request = new HttpRequestMessage(HttpMethod.Get, authorisationURL);

            Helpers.AuthServer.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var response = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = response.Headers.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect query
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(response.Headers.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            return Helpers.Web.CreateHttpClient(Constants.Certificates.CertificateFilename, Constants.Certificates.CertificatePassword, false);
        }

        private static string GenerateErrorRedirectFragment(AuthoriseException error)
        {
            return $"error={error.Error} & error_description={error.ErrorDescription}&state=";
        }
    }
}
