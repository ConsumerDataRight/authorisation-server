// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using CdrAuthServer.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using Dapper;
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
using static IdentityModel.ClaimComparer;
using static IdentityModel.OidcConstants;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests.JARM
{
    // JARM - DCR related tests
    public partial class US44264_CdrAuthServer_JARM_DCR : BaseTest, IClassFixture<BaseFixture>
    {
        public const string HYBRIDFLOW_RESPONSETYPE = "code id_token";
        public const string HYBRIDFLOW_GRANTTYPES = "authorization_code,client_credentials";
        public const string AUTHORIZATIONCODEFLOW_RESPONSETYPE = "code";
        public const string AUTHORIZATIONCODEFLOW_GRANTTYPES = "authorization_code";
        public const string ACF_HF_RESPONSETYPES = AUTHORIZATIONCODEFLOW_RESPONSETYPE + "," + HYBRIDFLOW_RESPONSETYPE;
        public const string ACF_HF_GRANTTYPES = "authorization_code,client_credentials";

        public const string IDTOKEN_SIGNED_RESPONSE_ALG_ES256 = "ES256";
        public const string IDTOKEN_SIGNED_RESPONSE_ALG_PS256 = "PS256";
        public const string IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP = "RSA-OAEP";
        public const string IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP_256 = "RSA-OAEP-256";
        public const string IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM = "A256GCM";
        public const string IDTOKEN_ENCRYPTED_RESPONSE_ENC_A128CBC_HS256 = "A128CBC-HS256";

        public const string AUTHORIZATION_SIGNED_RESPONSE_ALG_ES256 = "ES256";
        public const string AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256 = "PS256";
        public const string AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP = "RSA-OAEP";
        public const string AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP_256 = "RSA-OAEP-256";
        public const string AUTHORIZATION_ENCRYPTED_RESPONSE_ENC_A256GCM = "A256GCM";
        public const string AUTHORIZATION_ENCRYPTED_RESPONSE_ENC_A128CBC_HS256 = "A128CBC-HS256";

        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IRegisterSsaService _registerSSAService;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;
        private readonly IApiServiceDirector _apiServiceDirector;

        private readonly string _latestSSAVersion = "3";

        public US44264_CdrAuthServer_JARM_DCR(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IRegisterSsaService registerSSAService, IDataHolderRegisterService dataHolderRegisterService, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config, IApiServiceDirector apiServiceDirector)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _registerSSAService = registerSSAService ?? throw new ArgumentNullException(nameof(registerSSAService));
            _dataHolderRegisterService = dataHolderRegisterService ?? throw new ArgumentNullException(nameof(dataHolderRegisterService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        public async Task AC01_MDHJWKS_HF_AC1_POST_With_ShouldRespondWith_201Created(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            //TODO: I think commented code below was looking for a clean way to replace TestPOST. Continue this
            //var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SOFTWAREPRODUCT_ID);

            //Helpers.AuthServer.DataHolder_PurgeAuthServer(_options);
            //var ssa = await _registerSSAService.GetSSA(Constants.Brands.BRANDID, Constants.SoftwareProducts.SOFTWAREPRODUCT_ID, "3");

            //// Act
            //HttpResponseMessage responseMessage = await RegisterSoftwareProduct(
            //    responseType,
            //    grantTypes.Split(','),  // MJS - CDRAuthServer (see log) fails with:-  validation failed: [{"MemberNames": ["GrantTypes"], "ErrorMessage": "The 'grant_types' claim value must contain the 'authorization_code' value.", "$type": "ValidationResult"}]
            //    null,
            //    ssa,
            //    GetIdTokenEncryptedResponseAlg(responseType),
            //    GetIdTokenEncryptedResponseEnc(responseType)
            //    );

            //// Assert
            //using (new AssertionScope(BaseTestAssertionStrategy))
            //{
            //    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
            //    {
            //        return;
            //    }

            //    Assertions.Assert_HasContentType_ApplicationJson(responseMessage.Content);

            //    await AssertDCR(
            //        responseType,
            //        grantTypes.Split(','),
            //        authorizationSignedResponseAlg,
            //        responseMessage,
            //        registerDetails,
            //        ssa,
            //        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
            //        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));

            //    testPostDelegate(responseMessage, registerDetails, ssa);
            //}

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','), // MJS - CDRAuthServer (see log) fails with:-  validation failed: [{"MemberNames": ["GrantTypes"], "ErrorMessage": "The 'grant_types' claim value must contain the 'authorization_code' value.", "$type": "ValidationResult"}]
                authorization_signed_response_alg: null,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "ES256")]
        public async Task AC02_MDHJWKS_ACF_AC20_POST_With_AuthSigningAlgES256_ShouldRespondWith_201Created(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC03_MDHJWKS_ACF_AC21_POST_With_AuthSigningAlgPS256_ShouldRespondWith_201Created(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);
            
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, Constants.Null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "")]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "none")]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "RS256")]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "Foo")]
        public async Task AC05_MDHJWKS_ACF_AC24_POST_With_InvalidAuthSigningAlg_ShouldRespondWith_400BadRequest(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            //Arrange
            AuthoriseException expectedError;
            if (string.IsNullOrEmpty(authorizationSignedResponseAlg) || authorizationSignedResponseAlg == Constants.Null)
            {
                expectedError = new AuthorizationSignedResponseAlgClaimMissingException();
            }
            else
            {
                expectedError = new AuthorizationSignedResponseAlgClaimInvalidException();
            }

            ////Act
            //var responseMessage = await TestRegisterSoftwareProduct(
            //   responseType: responseType,
            //   grant_types: grantTypes.Split(','),
            //   authorization_signed_response_alg: authorizationSignedResponseAlg
            //   );

            ////Asert
            //await AssertError(responseMessage, expectedError);

            //Act/Assert
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    await Assertions.AssertErrorAsync(responseMessage, expectedError);
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC06_MDHJWKS_AC2_POST_With_SoftwareIDAlreadyRegistered_ShouldRespondWith_400BadRequest(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            // Arrange
            AuthoriseException expectedError = new DuplicateRegistrationForSoftwareIdException();

            //var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SOFTWAREPRODUCT_ID);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            // register software product
            HttpResponseMessage responseMessageSetup = await RegisterSoftwareProduct(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                ssa,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));

            // Check it was registered
            if (responseMessageSetup.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"expected Created - {await responseMessageSetup.Content.ReadAsStringAsync()}");
            }

            // Act - Now try and register it again
            HttpResponseMessage responseMessage = await RegisterSoftwareProduct(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                ssa,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(responseMessage, expectedError);
            }
        }

        // [Fact]
        internal Task AC07_MDHJWKS_AC3_POST_With_UnapprovedSSA_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC08_MDHJWKS_AC6_POST_With_InvalidSSAPayload_RedirectURI_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();  // MJS - SSA is signed by Register, would need to create invalid SSA and sign with Register cert?
        }

        // [Fact] 
        internal Task AC09_MDHJWKS_AC7_POST_With_InvalidSSAPayload_TokenEndpointAuthSigningALG_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();  // MJS - SSA is signed by Register, would need to create invalid SSA and sign with Register cert?
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        public async Task AC09_MDHJWKS_AC8_GET_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testGetDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC10_MDHJWKS_AC28_GET_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testGetDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC11_MDHJWKS_AC9_GET_With_ExpiredBearerToken_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testGetDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        startsWith: true);
                });
        }

        // [Theory] 
        // [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        // [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        internal Task AC12_MDHJWKS_AC10_GET_With_ClientIdNotRegistered_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        public async Task AC13_MDHJWKS_HF_AC11_PUT_With_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPutDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC14_MDHJWKS_ACF_AC31_PUT_With_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPutDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }


        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC15_MDHJWKS_AC12_PUT_With_InvalidRegistrationProperty_RedirectUri_ShouldRespondWith_400BadRequest(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            //Arrange
            const string REDIRECT_URI = "foo";
            var expectedError = new InvalidRedirectUriException(REDIRECT_URI);

            //Act-Assert
            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                redirectUrisForPut: new string[] { REDIRECT_URI },  // we are testing if invalid URI for PUT fails 

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPutDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    await Assertions.AssertErrorAsync(responseMessage, expectedError);
                });
        }


        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC16_MDHJWKS_AC13_PUT_With_ExpiredBearerToken_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPutDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        true);
                });
        }

        // [Theory] 
        // [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        // [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        internal Task AC17_MDHJWKS_AC14_PUT_With_ClientIDNotRegistered_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            throw new NotImplementedException("No AC");
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC18_MDHJWKS_AC15_DELETE_ShouldRespondWith_204NoContent(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestDELETE(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testDeleteDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.NoContent))
                    {
                        return;
                    }

                    await Assertions.AssertHasNoContent(responseMessage.Content);
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC19_MDHJWKS_AC17_DELETE_WithExpiredBearerToken_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestDELETE(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testDeleteDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        startsWith: true);
                });
        }

        [Theory, CombinatorialData]
        public async Task AC20_MDHJWKS_ACF_AC32_Happy_Path_POST_to_register_endpoint_JARM_Encryption(
            [CombinatorialValues(AUTHORIZATION_SIGNED_RESPONSE_ALG_ES256, AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256)]
            string authorizationSignedResponseAlg,
            [CombinatorialValues(AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP, AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP_256)]
            string authorizationEncryptedResponseAlg,
            [CombinatorialValues(AUTHORIZATION_ENCRYPTED_RESPONSE_ENC_A256GCM, AUTHORIZATION_ENCRYPTED_RESPONSE_ENC_A128CBC_HS256)]
            string authorizationEncryptedResponseEnc)
        {
            await TestEncryption(
                responseType: AUTHORIZATIONCODEFLOW_RESPONSETYPE,
                grantTypes: AUTHORIZATIONCODEFLOW_GRANTTYPES,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                authorizationEncryptedResponseAlg: authorizationEncryptedResponseAlg,
                authorizationEncryptedResponseEnc: authorizationEncryptedResponseEnc,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP,
                idTokenEncryptedResponseEnc: IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM);
        }

        // [Fact] 
        internal Task AC21_MDHJWKS_ACF_AC33_Reject_POST_to_register_endpoint_Invalid_authorization_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC22_MDHJWKS_ACF_AC34_Reject_POST_to_register_endpoint_Invalid_authorization_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        [Theory, CombinatorialData]
        public async Task AC23_MDHJWKS_ACF_AC35_Happy_Path_POST_to_register_endpoint_JARM_Encryption_alg_specified_without_enc(
            [CombinatorialValues(AUTHORIZATION_SIGNED_RESPONSE_ALG_ES256, AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256)]
            string authorizationSignedResponseAlg,
            [CombinatorialValues(AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP, AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP_256)]
            string authorizationEncryptedResponseAlg)
        {
            await TestEncryption(
                responseType: AUTHORIZATIONCODEFLOW_RESPONSETYPE,
                grantTypes: AUTHORIZATIONCODEFLOW_GRANTTYPES,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                authorizationEncryptedResponseAlg: authorizationEncryptedResponseAlg,
                authorizationEncryptedResponseEnc: null,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP,
                idTokenEncryptedResponseEnc: IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM);
        }

        // [Fact] 
        internal Task AC24_MDHJWKS_ACF_AC36_Reject_POST_to_register_endpoint_Omit_authorization_encrypted_response_alg_when_enc_is_provided()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, IDTOKEN_SIGNED_RESPONSE_ALG_PS256, null, null, null, null)]
        public async Task AC25_MDHJWKS_ACF_AC37_Happy_Path_ACF_only_DCR_without_id_token_encryption(
            string responseType,
            string grantTypes,
            string idTokenSignedResponseAlg,
            string idTokenEncryptedResponseAlg,
            string idTokenEncryptedResponseEnc,
            string expectedIdTokenEncryptedResponseAlg,
            string expectedIdTokenEncryptedResponseEnc)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}, {P4}={V4}, {P5}={V5}, {P6}={V6}, {P7}={V7}.",
                nameof(responseType), responseType,
                nameof(grantTypes), grantTypes,
                nameof(idTokenSignedResponseAlg), idTokenSignedResponseAlg,
                nameof(idTokenEncryptedResponseAlg), idTokenEncryptedResponseAlg,
                nameof(idTokenEncryptedResponseEnc), idTokenEncryptedResponseEnc,
                nameof(expectedIdTokenEncryptedResponseAlg), expectedIdTokenEncryptedResponseAlg,
                nameof(expectedIdTokenEncryptedResponseEnc), expectedIdTokenEncryptedResponseEnc
                );

            await TestEncryption(
                responseType: responseType,
                grantTypes: grantTypes,
                authorizationSignedResponseAlg: AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc,
                expectedIdTokenEncryptedResponseAlg: expectedIdTokenEncryptedResponseAlg,
                expectedIdTokenEncryptedResponseEnc: expectedIdTokenEncryptedResponseEnc);
        }

        [Fact]
        public async Task AC26_MDHJWKS_ACF_AC38_Happy_Path_ACF_only_DCR_with_id_token_encryption()
        {
            await TestEncryption(
                responseType: AUTHORIZATIONCODEFLOW_RESPONSETYPE,
                grantTypes: AUTHORIZATIONCODEFLOW_GRANTTYPES,
                authorizationSignedResponseAlg: AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP,
                idTokenEncryptedResponseEnc: IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM);
        }

        // [Fact] 
        internal Task AC28_MDHJWKS_ACF_AC40_Reject_ACF_DCR_invalid_id_token_signed_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC29_MDHJWKS_ACF_AC41_Reject_ACF_DCR_invalid_id_token_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC30_MDHJWKS_ACF_AC42_Reject_ACF_DCR_invalid_id_token_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC31_MDHJWKS_HF_AC43_Reject_HF_only_DCR_without_id_token_encryption()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AC32_MDHJWKS_HF_AC44_Happy_Path_HF_only_DCR_with_id_token_encryption()
        {
            await TestEncryption(
                responseType: HYBRIDFLOW_RESPONSETYPE,
                grantTypes: HYBRIDFLOW_GRANTTYPES,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP,
                idTokenEncryptedResponseEnc: IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM);
        }

        // [Fact] 
        internal Task AC34_MDHJWKS_HF_AC46_Reject_HF_DCR_invalid_id_token_signed_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC35_MDHJWKS_HF_AC47_Reject_HF_DCR_invalid_id_token_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact]   
        internal Task AC36_MDHJWKS_HF_AC48_Reject_HF_DCR_invalid_id_token_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        internal Task AC37_MDHJWKS_ACF_HF_AC49_Reject_ACF_HF_DCR_without_id_token_encryption()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task AC38_MDHJWKS_ACF_HF_AC50_Happy_Path_ACF_HF_DCR_with_id_token_encryption()
        {
            await TestEncryption(
                responseType: ACF_HF_RESPONSETYPES,
                grantTypes: ACF_HF_GRANTTYPES,
                authorizationSignedResponseAlg: AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP,
                idTokenEncryptedResponseEnc: IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM);
        }

        // [Fact]
        internal Task AC40_MDHJWKS_ACF_HF_AC52_Reject_ACF_HF_DCR_invalid_id_token_signed_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact]
        internal Task AC41_MDHJWKS_ACF_HF_AC53_Reject_ACF_HF_DCR_invalid_id_token_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact]
        internal Task AC42_MDHJWKS_ACF_HF_AC54_Reject_ACF_HF_DCR_invalid_id_token_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, "foo")]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, "foo")]
        public async Task AC43_MDHJWKS_ACF_HF_AC55_With_InvalidGrantType_ShouldRespondWith_400BadRequest(string responseType, string grantTypes)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes);

            var expectedError = new GrantTypesMissingAuthorizationCodeException();

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: null,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    await Assertions.AssertErrorAsync(responseMessage, expectedError);
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null, null)]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, "none", null)]
        public async Task AC44_MDHJWKS_HF_AC56_POST_With_AuthorizationSignedResponseAlg_NotProvided_ShouldRespondWith_201Created(
            string responseType,
            string grantTypes,
            string authorizationSignedResponseAlg,
            string expected_authorization_signed_response_alg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg, nameof(expected_authorization_signed_response_alg), expected_authorization_signed_response_alg);

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: null,

                idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        expected_authorization_signed_response_alg: expected_authorization_signed_response_alg,
                        idTokenEncryptedResponseAlg: GetIdTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: GetIdTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }

        /// <summary>
        /// return an IdTokenEncryptedResponseAlg if response type contains "id_token", otherwise return null
        /// </summary>
        private static string? GetIdTokenEncryptedResponseAlg(string responseType)
        {
            if (responseType.ToUpper().Contains("ID_TOKEN"))
            {
                return IDTOKEN_ENCRYPTED_RESPONSE_ALG_RSA_OAEP;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// return an IdTokenEncryptedResponseEnc if response type contains "id_token", otherwise return null
        /// </summary>
        private static string? GetIdTokenEncryptedResponseEnc(string responseType)
        {
            if (responseType.ToUpper().Contains("ID_TOKEN"))
            {
                return IDTOKEN_ENCRYPTED_RESPONSE_ENC_A256GCM;
            }
            else
            {
                return null;
            }
        }

        // Lookup details for software product from Register
        private dynamic LookupRegisterDetails(string softwareProductId)
        {
            using var connection = new SqlConnection(_options.REGISTER_CONNECTIONSTRING);
            connection.Open();

            var sql = @"
                select 
                    le.LegalEntityId, le.LegalEntityName,
                    b.BrandId, b.BrandName,
                    sp.* 
                from SoftwareProduct sp 
                    left outer join brand b on b.BrandId = sp.BrandId
                    left outer join participation p on p.ParticipationId = b.ParticipationId
                    left outer join legalentity le on le.LegalEntityId = p.LegalEntityId
                where 
                    sp.SoftwareProductId = @SoftwareProductId";

            return connection.QuerySingle(sql, new { SoftwareProductId = softwareProductId }) ?? throw new NullReferenceException();
        }

        private delegate void TestPOSTDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        private async Task TestPOST(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            TestPOSTDelegate testPostDelegate,

            string? authorization_encrypted_response_alg = null,
            string? authorization_encrypted_response_enc = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            // Act
            HttpResponseMessage responseMessage = await RegisterSoftwareProduct(
                responseType,
                grant_types,
                authorization_signed_response_alg,
                ssa,
                authorization_encrypted_response_alg,
                authorization_encrypted_response_enc,
                idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                testPostDelegate(responseMessage, registerDetails, ssa);
            }
        }

        private async Task<HttpResponseMessage> TestRegisterSoftwareProduct(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            string? authorization_encrypted_response_alg = null,
            string? authorization_encrypted_response_enc = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            // Act
            HttpResponseMessage responseMessage = await RegisterSoftwareProduct(
                responseType,
                grant_types,
                authorization_signed_response_alg,
                ssa,
                authorization_encrypted_response_alg,
                authorization_encrypted_response_enc,
                idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc);

            return responseMessage;

        }


        private delegate void TestPUTDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        private async Task TestPUT(
            string responseType,
            string[] grantTypes,
            string? authorizationSignedResponseAlg,
            string[]? redirectUrisForPut = null,
            bool expiredAccessToken = false,
            TestPUTDelegate? testPutDelegate = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            // Arrange 
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var _registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grantTypes,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);
            var dcrResponseMessage = await _dataHolderRegisterService.RegisterSoftwareProduct(_registrationRequest);

            if (dcrResponseMessage.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Expected Created but {dcrResponseMessage.StatusCode} - {await dcrResponseMessage.Content.ReadAsStringAsync()}");
            }
            var dcrResponse = JsonConvert.DeserializeObject<DcrResponse>(await dcrResponseMessage.Content.ReadAsStringAsync());

            // Act 
            var registrationRequestForPut = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grantTypes,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                redirectUris: redirectUrisForPut,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);

            var accessToken = await new DataHolderAccessToken(dcrResponse?.client_id, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken(expiredAccessToken);

            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, registrationRequestForPut, HttpMethod.Put, dcrResponse?.client_id ?? string.Empty);
            var responseMessage = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                testPutDelegate(responseMessage, registerDetails, ssa);
            }
        }

        private delegate void TestDELETEDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        private async Task TestDELETE(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            bool expiredAccessToken = false,
            TestDELETEDelegate? testDeleteDelegate = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            // Arrange 
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var _registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grant_types,
                authorizationSignedResponseAlg: authorization_signed_response_alg,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);

            var dcrResponseMessage = await _dataHolderRegisterService.RegisterSoftwareProduct(_registrationRequest);

            if (dcrResponseMessage.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Expected Created but {dcrResponseMessage.StatusCode} - {await dcrResponseMessage.Content.ReadAsStringAsync()}");
            }
            var dcrResponse = JsonConvert.DeserializeObject<DcrResponse>(await dcrResponseMessage.Content.ReadAsStringAsync());

            // Act 
            var accessToken = await new DataHolderAccessToken(dcrResponse?.client_id, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken(expiredAccessToken);

            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, null, HttpMethod.Delete, dcrResponse?.client_id ?? string.Empty);
            var responseMessage = await api.SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                testDeleteDelegate(responseMessage, registerDetails, ssa);
            }
        }

        private delegate void TestGETDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        private async Task TestGET(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            bool expiredAccessToken = false,
            TestGETDelegate? testGetDelegate = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            HttpResponseMessage dcrResponseMessage = await RegisterSoftwareProduct(
                responseType,
                grant_types,
                authorization_signed_response_alg,
                ssa,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);

            if (dcrResponseMessage.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Expected Created but {dcrResponseMessage.StatusCode} - {await dcrResponseMessage.Content.ReadAsStringAsync()}");
            }

            var dcrResponse = JsonConvert.DeserializeObject<DcrResponse>(await dcrResponseMessage.Content.ReadAsStringAsync());

            var accessToken = await new DataHolderAccessToken(dcrResponse?.client_id, _options.DH_MTLS_GATEWAY_URL, _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE).GetAccessToken(expiredAccessToken);

            // Act
            var api = _apiServiceDirector.BuildDataholderRegisterAPI(accessToken, null, HttpMethod.Get, dcrResponse?.client_id ?? string.Empty);
            var responseMessage = await api.SendAsync(allowAutoRedirect: false);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                testGetDelegate(responseMessage, registerDetails, ssa);
            }
        }

        private async Task<HttpResponseMessage> RegisterSoftwareProduct(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            string ssa,
            string? authorization_encrypted_response_alg = null,
            string? authorization_encrypted_response_enc = null,

            string? idTokenSignedResponseAlg = null,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grant_types,
                authorizationSignedResponseAlg: authorization_signed_response_alg,
                authorizationEncryptedResponseAlg: authorization_encrypted_response_alg,
                authorizationEncryptedResponseEnc: authorization_encrypted_response_enc,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);

            var responseMessage = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            return responseMessage;
        }

        private async Task<DcrResponse> AssertDCR(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            HttpResponseMessage responseMessage,
            dynamic registerDetails,
            string ssa,
            string? authorizationEncryptedResponseAlg = null,
            string? authorizationEncryptedResponseEnc = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null,

            string? expected_authorization_signed_response_alg = null)
        {
            var json = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<DcrResponse>(json)??throw new ArgumentNullException(nameof(responseMessage));
            string[] expectedResponseTypes = responseType.Contains(',') ? responseType.Split(",") : new string[] { responseType };
            response.response_types.Should().BeEquivalentTo(expectedResponseTypes);

            response.grant_types.Should().BeEquivalentTo(grant_types);

            if (response.response_types?.Contains("code") ?? false)
            {
                if (authorization_signed_response_alg != null || expected_authorization_signed_response_alg != null)
                {
                    response.authorization_signed_response_alg.Should().Be(expected_authorization_signed_response_alg ?? authorization_signed_response_alg);
                }

                if (_authServerOptions.JARM_ENCRYPTION_ON)
                {
                    if (authorizationEncryptedResponseAlg != null)
                    {
                        response.authorization_encrypted_response_alg.Should().Be(authorizationEncryptedResponseAlg);
                    }
                    if (authorizationEncryptedResponseEnc != null)
                    {
                        response.authorization_encrypted_response_enc.Should().Be(authorizationEncryptedResponseEnc);
                    }
                }
                else
                {
                    response.authorization_encrypted_response_alg.Should().BeNull();
                    response.authorization_encrypted_response_enc.Should().BeNull();
                }
            }
            else
            {
                response.authorization_signed_response_alg.Should().BeNull();
                response.authorization_encrypted_response_alg.Should().BeNull();
                response.authorization_encrypted_response_enc.Should().BeNull();
            }

            response.client_id.Should().NotBeNullOrEmpty();
            response.client_id_issued_at.Should().NotBeNullOrEmpty();
            response.client_name.Should().Be(registerDetails.SoftwareProductName);
            response.client_description.Should().Be(registerDetails.SoftwareProductDescription);
            response.client_uri.Should().Be(registerDetails.ClientUri);

            response.org_id.Should().Be(registerDetails.BrandId.ToString());
            response.org_name.Should().Be(registerDetails.BrandName);

            response.redirect_uris.Should().BeEquivalentTo(new string[] { registerDetails.RedirectUris });

            response.logo_uri.Should().Be(registerDetails.LogoUri);
            response.tos_uri.Should().Be(registerDetails.TosUri);
            response.policy_uri.Should().Be(registerDetails.PolicyUri);
            response.jwks_uri.Should().Be(registerDetails.JwksUri);
            response.revocation_uri.Should().Be(registerDetails.RevocationUri);

            response.sector_identifier_uri.Should().Be(registerDetails.SectorIdentifierUri);

            response.recipient_base_uri.Should().Be(registerDetails.RecipientBaseUri);

            response.token_endpoint_auth_method.Should().Be("private_key_jwt");
            response.token_endpoint_auth_signing_alg.Should().Be("PS256");

            response.application_type.Should().Be("web");

            response.id_token_signed_response_alg.Should().Be(idTokenSignedResponseAlg);
            response.id_token_encrypted_response_alg.Should().Be(idTokenEncryptedResponseAlg);
            response.id_token_encrypted_response_enc.Should().Be(idTokenEncryptedResponseEnc);

            response.request_object_signing_alg.Should().Be("PS256");

            response.software_statement.Should().Be(ssa);
            response.software_id.Should().Be(Constants.SoftwareProducts.SoftwareProductId);
            response.software_roles.Should().Be("data-recipient-software-product");

            response.scope.Should().ContainAll("openid", "bank:accounts.basic:read", "cdr:registration");

            return response;
        }

        //private async Task AssertError(HttpResponseMessage responseMessage, HttpStatusCode expectedStatusCode, string expectedResponse)
        //{
        //    responseMessage.StatusCode.Should().Be(expectedStatusCode);

        //    Assertions.Assert_HasContentType_ApplicationJson(responseMessage.Content);

        //    var json = await responseMessage.Content.ReadAsStringAsync();

        //    await Assertions.Assert_HasContent_Json(expectedResponse, responseMessage.Content);
        //}

        //private async Task AssertError(HttpResponseMessage responseMessage, AuthoriseException expectedError)
        //{
        //    responseMessage.StatusCode.Should().Be(expectedError.StatusCode);

        //    Assertions.Assert_HasContentType_ApplicationJson(responseMessage.Content);

        //    var responseContent = await responseMessage.Content.ReadAsStringAsync();
        //    var receivedError = JsonConvert.DeserializeObject<AuthError>(responseContent);

        //    receivedError.Should().NotBeNull();
        //    receivedError.Description.Should().Be(expectedError.ErrorDescription);
        //    receivedError.Code.Should().Be(expectedError.Error);
        //}

        static async Task<bool> AssertExpectedResponseCode(HttpResponseMessage responseMessage, HttpStatusCode expectedStatusCode)
        {
            responseMessage.StatusCode.Should().Be(expectedStatusCode);
            if (responseMessage.StatusCode == expectedStatusCode)
            {
                return true;
            }

            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            responseContent.Should().NotBe(responseContent);
            return false;
        }

        private async Task TestEncryption(
          string responseType,
          string grantTypes,
          string? authorizationSignedResponseAlg = null,
          string? authorizationEncryptedResponseAlg = null,
          string? authorizationEncryptedResponseEnc = null,
          string? idTokenSignedResponseAlg = null,
          string? idTokenEncryptedResponseAlg = null,
          string? idTokenEncryptedResponseEnc = null,
          string? expectedIdTokenEncryptedResponseEnc = null,
          string? expectedIdTokenEncryptedResponseAlg = null
          )
        {
            await TestPOST(
                responseType: responseType,

                grant_types: grantTypes.Split(','),

                authorization_signed_response_alg: authorizationSignedResponseAlg,
                authorization_encrypted_response_alg: authorizationEncryptedResponseAlg,
                authorization_encrypted_response_enc: authorizationEncryptedResponseEnc,

                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc,

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);
                    await AssertDCR(responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        authorizationEncryptedResponseAlg,
                        authorizationEncryptedResponseEnc,
                        idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                        idTokenEncryptedResponseAlg: expectedIdTokenEncryptedResponseAlg ?? idTokenEncryptedResponseAlg,
                        idTokenEncryptedResponseEnc: expectedIdTokenEncryptedResponseEnc ?? idTokenEncryptedResponseEnc);

                });
        }

    }
}
