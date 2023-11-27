// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
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
using static IdentityModel.OidcConstants;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests.JARM
{
    // JARM - PAR related tests
    // https://cdr-internal.atlassian.net/wiki/spaces/PT/pages/47513616/MDH+PAR+endpoint+Acceptance+Criteria
    public class US44264_CdrAuthServer_JARM_PAR : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly ISqlQueryService _sqlQueryService;

        public US44264_CdrAuthServer_JARM_PAR(IOptions<TestAutomationOptions> options, IDataHolderParService dataHolderParService, ISqlQueryService sqlQueryService, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
        }

        public class PARResponse
        {
            public string? request_uri { get; set; }
            public string? expires_in { get; set; }
        }

        public delegate void TestPOSTDelegate(HttpResponseMessage responseMessage);
        public async Task TestPOST(
            ResponseType? responseType = null,
            ResponseMode? responseMode = ResponseMode.Fragment,
            string? clientId = null,
            string? clientAssertionType = Constants.ClientAssertionType,
            string? clientAssertion = null,
            TestPOSTDelegate? testPostDelegate = null)
        {
            // Arrange 

            // Act
            var responseMessage = await _dataHolderParService.SendRequest(
                scope: _options.SCOPE,
                responseType: responseType,
                responseMode: responseMode,
                clientId: clientId,
                clientAssertionType: clientAssertionType,
                clientAssertion: clientAssertion
            );

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                if (testPostDelegate != null)
                {
                    testPostDelegate(responseMessage);
                }
            }
        }

        [Theory]
        [InlineData(ResponseType.CodeIdToken, ResponseMode.Fragment)] //Hybrid mode
        [InlineData(ResponseType.CodeIdToken, ResponseMode.FormPost)] //Hybrid mode
        [InlineData(ResponseType.Code, ResponseMode.Jwt)] //ACF mode
        public async Task AC01_MDHPAR_AC01_HappyPath_ShouldRespondWith_201Created(ResponseType responseType, ResponseMode responseMode)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(responseType), responseType, nameof(responseMode), responseMode);

            // Arrange

            // Act
            var response = await _dataHolderParService.SendRequest(
                scope: _options.SCOPE,
                responseType: responseType,
                responseMode: responseMode
            );

            var responseText = await response.Content.ReadAsStringAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created, responseText);

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

        // [Fact]
        public async Task AC02_MDHPAR_AC04_HappyPath_AmendExistingArrangement_ShouldRespondWith_200OK()
        {
            throw new NotImplementedException();
        }

        // [Fact]
        public async Task AC03_MDHPAR_AC06_WithUnownedArrangementId_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AC05_MDHPAR_AC08_WithUnknownClientId_ShouldRespondWith_400BadRequest()
        {
            //Arrange
            var expectedError = new ClientNotFoundException();

            //Act-Assert
            await TestPOST(
                //grantType: "client_credentials",
                clientId: "foo",
                clientAssertionType: "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        [Fact]
        public async Task AC06_MDHPAR_AC09_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest()
        {
            //Arrange
            var expectedError = new InvalidClientAssertionTypeException();

            //Act-Assert
            await TestPOST(
                //grantType: "client_credentials",
                clientId: _options.LastRegisteredClientId,
                clientAssertionType: "foo",
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        [Fact]
        public async Task AC07_MDHPAR_AC10_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest()
        {
            //Arrange
            var expectedError = new InvalidClientAssertionFormatException();

            //Act-Assert
            await TestPOST(
                //grantType: "client_credentials",
                clientId: _options.LastRegisteredClientId,
                clientAssertionType: "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                clientAssertion: "foo",
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        // [Fact]
        public async Task AC08_MDHPAR_AC11_WithClientAssertionAssociatedWithDifferentClientId_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(ResponseMode.TestOnlyFoo)]
        [InlineData(ResponseMode.QueryJwt)]
        [InlineData(ResponseMode.FragmentJwt)]
        [InlineData(ResponseMode.FormPostJwt)]
        public async Task AC09_MDHPAR_AC12_HF_WithInvalidResponseMode_ShouldRespondWith_400BadRequest(ResponseMode responseMode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(responseMode), responseMode);

            //Arrange
            var expectedError = new UnsupportedResponseModeException();

            //Act-Assert
            await TestPOST(
                responseType: ResponseType.CodeIdToken,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        [Theory]
        [InlineData(null)]
        [InlineData(ResponseMode.TestOnlyFoo)]
        [InlineData(ResponseMode.Query)]
        [InlineData(ResponseMode.QueryJwt)]
        [InlineData(ResponseMode.FragmentJwt)]
        [InlineData(ResponseMode.FormPostJwt)]
        public async Task AC10_MDHPAR_AC13_ACF_WithUnsupportedResponseMode_ShouldRespondWith_400BadRequest(ResponseMode? responseMode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(responseMode), responseMode);

            //Arrange
            var expectedError = new UnsupportedResponseModeException();

            //Act-Assert
            await TestPOST(
                responseType: ResponseType.Code,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        [Theory]
        [InlineData(ResponseMode.Fragment)]
        [InlineData(ResponseMode.FormPost)]
        public async Task AC10_MDHPAR_AC13_ACF_WithInvalidResponseMode_ShouldRespondWith_400BadRequest(ResponseMode responseMode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(responseMode), responseMode);

            //Arrange
            var expectedError = new InvalidResponseModeForResponseTypeException();

            //Act-Assert
            await TestPOST(
                responseType: ResponseType.Code,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        [Theory]
        [InlineData(ResponseType.TestOnlyFoo)]
        [InlineData(ResponseType.TestOnlyToken)]
        [InlineData(ResponseType.TestOnlyCodeToken)]
        [InlineData(ResponseType.TestOnlyIdTokenToken)]
        [InlineData(ResponseType.TestOnlyCodeIdTokenToken)]
        [InlineData(ResponseType.TestOnlyCodeFoo)]
        [InlineData(ResponseType.TestOnlyCodeIdTokenFooo)]
        public async Task AC13_MDHPAR_AC15_WithInvalidResponseType_ShouldRespondWith_400BadRequest(ResponseType responseType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(responseType), responseType);

            //Arrange
            var expectedError = new UnsupportedResponseTypeException();

            //Act-Assert
            await TestPOST(
                responseType: responseType,
                responseMode: ResponseMode.Fragment,
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        //TODO: Missing test for AC14

        [Fact]
        public async Task AC15_MDHPAR_AC16_WithMissingResponseType_ShouldRespondWith_400BadRequest()
        {
            //Arrange
            var expectedError = new MissingResponseTypeException();

            //Act-Assert
            await TestPOST(
                responseType: null,
                responseMode: ResponseMode.Fragment,
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }

        [Theory]
        [InlineData(ResponseMode.Jwt)]
        public async Task AC16_MDHPAR_AC17_HF_WithInvalidResponseMode_ShouldRespondWith_400BadRequest(ResponseMode responseMode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(responseMode), responseMode);

            //Arrange
            var expectedError = new InvalidResponseModeForResponseTypeException();

            //Act-Assert
            await TestPOST(
                responseType: ResponseType.CodeIdToken,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    await Assertions.AssertErrorAsync(response, expectedError);
                });
        }
    }
}
