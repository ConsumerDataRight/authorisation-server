using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15586_CdrAuthServer_Registration_GET : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IApiServiceDirector _apiServiceDirector;
        private readonly string _clientId = null!;

        public US15221_US12969_US15586_CdrAuthServer_Registration_GET(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, ISqlQueryService sqlQueryService, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
            _clientId = _options.LastRegisteredClientId;
        }

        [Fact]
        public async Task AC08_Get_WithValidClientId_ShouldRespondWith_200OK_Profile()
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken(_clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken();

            // Act
            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: null, httpMethod: HttpMethod.Get, clientId: _clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task AC09_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken(_clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken(true);

            // Act
            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: null, httpMethod: HttpMethod.Get, clientId: _clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(
                        @"Bearer error=""invalid_token"", error_description=""The token expired at '05/16/2022 03:04:03'""",
                        response.Headers, "WWW-Authenticate");
                }
            }
        }

        [Fact]
        public async Task AC10_Get_WithInvalidClientId_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader()
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken(_clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken();

            // Act
            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: null, httpMethod: HttpMethod.Get, clientId: Constants.GuidFoo);
            var response = await api.SendAsync(allowAutoRedirect: false);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                {
                    // Assert - Check error response
                    // TODO - replace with authorise exception
                    Assertions.AssertHasHeader(
                        @"Bearer error=""invalid_request"", error_description=""The client is unknown""",
                        response.Headers,
                        "WWW-Authenticate",
                        true); // starts with
                }
            }
        }
    }
}
