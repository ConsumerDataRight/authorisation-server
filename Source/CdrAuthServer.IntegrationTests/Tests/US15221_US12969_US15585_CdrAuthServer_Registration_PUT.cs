using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15585_CdrAuthServer_Registration_PUT : BaseTest, IClassFixture<BaseFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US15221_US12969_US15585_CdrAuthServer_Registration_PUT(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IDataHolderRegisterService dataHolderRegisterService, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _dataHolderRegisterService = dataHolderRegisterService ?? throw new ArgumentNullException(nameof(dataHolderRegisterService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        // Purge database, register product and return SSA JWT / registration json / clientId of registered software product
        private async Task<(string ssa, string registration, string clientId)> Arrange()
        {
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            return await _dataHolderRegisterService.RegisterSoftwareProduct(responseType: "code,code id_token", authorizationSignedResponseAlg: "PS256");
        }

        [Fact]
        public async Task AC11_Put_WithValidSoftwareProduct_ShouldRespondWith_200OK_UpdatedProfile()
        {
            // Arrange
            var (ssa, expectedResponse, clientId) = await Arrange();

            // Act
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: "code,code id_token",
                authorizationSignedResponseAlg: "PS256",
                authorizationEncryptedResponseAlg: "RSA-OAEP",
                authorizationEncryptedResponseEnc: "A128CBC-HS256"
                );

            var accessToken = await new DataHolderAccessToken(clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken();

            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: registrationRequest, httpMethod: HttpMethod.Put, clientId: clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // Assert - Check application/json
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    // Assert - Check json
                    await Assertions.AssertHasContentJson(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC12_Put_WithInvalidSoftwareProduct_ShouldRespondWith_400BadRequest_InvalidErrorResponse()
        {
            // Arrange
            var (ssa, _, clientId) = await Arrange();

            const string RedirectUri = "foo";
            var expectedError = new InvalidRedirectUriException(RedirectUri);

            // Act
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa,
                redirectUris: new string[] { RedirectUri });  // Invalid redirect uris

            var accessToken = await new DataHolderAccessToken(clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken();

            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: registrationRequest, httpMethod: HttpMethod.Put, clientId: clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC13_Put_WithExpiredAccessToken_ShouldRespondWith_401UnAuthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var (ssa, _, clientId) = await Arrange();

            var accessToken = await new DataHolderAccessToken(clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken(true);

            // Act
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa);

            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: registrationRequest, httpMethod: HttpMethod.Put, clientId: clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at '05/16/2022 03:04:03'""",
                        response.Headers, "WWW-Authenticate");
                }
            }
        }

        [Theory]
        [InlineData(Constants.GuidFoo)]
        public async Task AC14_Put_WithInvalidOrUnregisteredClientID_ShouldRespondWith_401Unauthorised_InvalidErrorResponse(string invalidClientId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(invalidClientId), invalidClientId);

            // Arrange
            var (ssa, _, clientId) = await Arrange();

            // Act
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa);

            var accessToken = await new DataHolderAccessToken(clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken();

            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: registrationRequest, httpMethod: HttpMethod.Put, clientId: invalidClientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(@"Bearer error=""invalid_request"", error_description=""The client is unknown""",
                       response.Headers, "WWW-Authenticate");
                }
            }
        }
    }
}