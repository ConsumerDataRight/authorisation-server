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
    public class US15221_US12969_US15587_CdrAuthServer_Registration_DELETE : BaseTest, IClassFixture<BaseFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US15221_US12969_US15587_CdrAuthServer_Registration_DELETE(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IDataHolderRegisterService dataHolderRegisterService, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
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

        // Purge database, register product and return SSA JWT and registration json
        private async Task<(string ssa, string registration, string clientId)> Arrange()
        {
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            return await _dataHolderRegisterService.RegisterSoftwareProduct();
        }

        [Fact]
        public async Task AC15_Delete_WithValidClientId_ShouldRespondWith_204NoContent_ProfileIsDeleted()
        {
            // Arrange
            var (_, _, clientId) = await Arrange();

            var accessToken = await new DataHolderAccessToken(clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken();

            // Act
            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: null, httpMethod: HttpMethod.Delete, clientId: clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                await Assertions.AssertHasNoContent2(response.Content);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    // do a get, should fail
                }
            }
        }

        [Fact]
        public async Task AC17_Delete_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var (_, _, clientId) = await Arrange();

            var accessToken = await new DataHolderAccessToken(clientId, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken(true);

            // Act
            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequest: null, httpMethod: HttpMethod.Delete, clientId: clientId);
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check statuscode
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
    }
}
