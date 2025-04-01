// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System.Net;
using CdrAuthServer.IntegrationTests.Models;
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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Xunit.DependencyInjection;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests.Tests.JARM
{
    // JARM - DCR related tests
    public partial class US44264_CdrAuthServer_JARM_DCR : BaseTest, IClassFixture<BaseFixture>
    {
        public const string AUTHORIZATIONCODEFLOW_RESPONSETYPE = "code";
        public const string AUTHORIZATIONCODEFLOW_GRANTTYPES = "authorization_code";

        public const string IDTOKEN_SIGNED_RESPONSE_ALG_ES256 = "ES256";
        public const string IDTOKEN_SIGNED_RESPONSE_ALG_PS256 = "PS256";

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
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "ES256")]
        public async Task AC02_MDHJWKS_ACF_AC20_POST_With_AuthSigningAlgES256_ShouldRespondWith_201Created(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
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
                        ssa);
                });
        }

        [Theory]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC03_MDHJWKS_ACF_AC21_POST_With_AuthSigningAlgPS256_ShouldRespondWith_201Created(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
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
                        ssa);
                });
        }

        [Theory]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, Constants.Null)]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "")]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "none")]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "RS256")]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "Foo")]
        public async Task AC05_MDHJWKS_ACF_AC24_POST_With_InvalidAuthSigningAlg_ShouldRespondWith_400BadRequest(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            // Arrange
            AuthoriseException expectedError;
            if (string.IsNullOrEmpty(authorizationSignedResponseAlg) || authorizationSignedResponseAlg == Constants.Null)
            {
                expectedError = new AuthorizationSignedResponseAlgClaimMissingException();
            }
            else
            {
                expectedError = new AuthorizationSignedResponseAlgClaimInvalidException();
            }

            // Act/Assert
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
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC06_MDHJWKS_AC2_POST_With_SoftwareIDAlreadyRegistered_ShouldRespondWith_400BadRequest(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            // Arrange
            AuthoriseException expectedError = new DuplicateRegistrationForSoftwareIdException();

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            // register software product
            var responseMessageSetup = await RegisterSoftwareProduct(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                ssa,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256);

            // Check it was registered
            if (responseMessageSetup.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"expected Created - {await responseMessageSetup.Content.ReadAsStringAsync()}");
            }

            // Act - Now try and register it again
            var responseMessage = await RegisterSoftwareProduct(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                ssa,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256);

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
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC10_MDHJWKS_AC28_GET_ShouldRespondWith_200OK(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
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
                        ssa);
                });
        }

        [Theory]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC11_MDHJWKS_AC9_GET_With_ExpiredBearerToken_ShouldRespondWith_401Unauthorized(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,
                testGetDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    _ = responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(
                        @"Bearer error=""invalid_token"", error_description=""The token expired at ",
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
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC14_MDHJWKS_ACF_AC31_PUT_With_ShouldRespondWith_200OK(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
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
                        ssa);
                });
        }

        [Theory]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC15_MDHJWKS_AC12_PUT_With_InvalidRegistrationProperty_RedirectUri_ShouldRespondWith_400BadRequest(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            // Arrange
            const string REDIRECT_URI = "foo";
            var expectedError = new InvalidRedirectUriException(REDIRECT_URI);

            // Act-Assert
            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                redirectUrisForPut: [REDIRECT_URI],  // we are testing if invalid URI for PUT fails
                testPutDelegate: async (
                    responseMessage, registerDetails, ssa) =>
                {
                    await Assertions.AssertErrorAsync(responseMessage, expectedError);
                });
        }

        [Theory]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC16_MDHJWKS_AC13_PUT_With_ExpiredBearerToken_ShouldRespondWith_401Unauthorized(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestPUT(
                responseType: responseType,
                grantTypes: grantTypes.Split(','),
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                testPutDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    _ = responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(
                        @"Bearer error=""invalid_token"", error_description=""The token expired at ",
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
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC18_MDHJWKS_AC15_DELETE_ShouldRespondWith_204NoContent(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestDELETE(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
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
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC19_MDHJWKS_AC17_DELETE_WithExpiredBearerToken_ShouldRespondWith_401Unauthorized(ResponseType responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes, nameof(authorizationSignedResponseAlg), authorizationSignedResponseAlg);

            await TestDELETE(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,
                testDeleteDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    _ = responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assertions.AssertHasHeader(
                        @"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        startsWith: true);
                });
        }

        [Theory]
        [CombinatorialData]
        public async Task AC20_MDHJWKS_ACF_AC32_Happy_Path_POST_to_register_endpoint_JARM_Encryption(
            [CombinatorialValues(AUTHORIZATION_SIGNED_RESPONSE_ALG_ES256, AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256)]
            string authorizationSignedResponseAlg,
            [CombinatorialValues(AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP, AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP_256)]
            string authorizationEncryptedResponseAlg,
            [CombinatorialValues(AUTHORIZATION_ENCRYPTED_RESPONSE_ENC_A256GCM, AUTHORIZATION_ENCRYPTED_RESPONSE_ENC_A128CBC_HS256)]
            string authorizationEncryptedResponseEnc)
        {
            await TestEncryption(
                responseType: ResponseType.Code,
                grantTypes: AUTHORIZATIONCODEFLOW_GRANTTYPES,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                authorizationEncryptedResponseAlg: authorizationEncryptedResponseAlg,
                authorizationEncryptedResponseEnc: authorizationEncryptedResponseEnc,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256);
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

        [Theory]
        [CombinatorialData]
        public async Task AC23_MDHJWKS_ACF_AC35_Happy_Path_POST_to_register_endpoint_JARM_Encryption_alg_specified_without_enc(
            [CombinatorialValues(AUTHORIZATION_SIGNED_RESPONSE_ALG_ES256, AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256)]
            string authorizationSignedResponseAlg,
            [CombinatorialValues(AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP, AUTHORIZATION_ENCRYPTED_RESPONSE_ALG_RSA_OAEP_256)]
            string authorizationEncryptedResponseAlg)
        {
            await TestEncryption(
                responseType: ResponseType.Code,
                grantTypes: AUTHORIZATIONCODEFLOW_GRANTTYPES,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                authorizationEncryptedResponseAlg: authorizationEncryptedResponseAlg,
                authorizationEncryptedResponseEnc: null,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256);
        }

        // [Fact]
        internal Task AC24_MDHJWKS_ACF_AC36_Reject_POST_to_register_endpoint_Omit_authorization_encrypted_response_alg_when_enc_is_provided()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(ResponseType.Code, AUTHORIZATIONCODEFLOW_GRANTTYPES, IDTOKEN_SIGNED_RESPONSE_ALG_PS256)]
        public async Task AC25_MDHJWKS_ACF_AC37_Happy_Path_ACF_only_DCR_without_id_token_encryption(
            ResponseType responseType,
            string grantTypes,
            string idTokenSignedResponseAlg)
        {
            Log.Information(
                "Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}, {P4}={V4}, {P5}={V5}, {P6}={V6}, {P7}={V7}.",
                nameof(responseType), responseType,
                nameof(grantTypes), grantTypes,
                nameof(idTokenSignedResponseAlg), idTokenSignedResponseAlg);

            await TestEncryption(
                responseType: responseType,
                grantTypes: grantTypes,
                authorizationSignedResponseAlg: AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg);
        }

        [Fact]
        public async Task AC26_MDHJWKS_ACF_AC38_Happy_Path_ACF_only_DCR_with_id_token_encryption()
        {
            await TestEncryption(
                responseType: ResponseType.Code,
                grantTypes: AUTHORIZATIONCODEFLOW_GRANTTYPES,
                authorizationSignedResponseAlg: AUTHORIZATION_SIGNED_RESPONSE_ALG_PS256,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256);
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
        [InlineData(ResponseType.Code, "foo")]
        public async Task AC43_MDHJWKS_ACF_HF_AC55_With_InvalidGrantType_ShouldRespondWith_400BadRequest(ResponseType responseType, string grantTypes)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(responseType), responseType, nameof(grantTypes), grantTypes);

            var expectedError = new GrantTypesMissingAuthorizationCodeException();

            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: null,
                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    await Assertions.AssertErrorAsync(responseMessage, expectedError);
                });
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
            ResponseType responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            TestPOSTDelegate testPostDelegate,
            string? authorization_encrypted_response_alg = null,
            string? authorization_encrypted_response_enc = null,
            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            // Act
            var responseMessage = await RegisterSoftwareProduct(
                responseType,
                grant_types,
                authorization_signed_response_alg,
                ssa,
                authorization_encrypted_response_alg,
                authorization_encrypted_response_enc,
                idTokenSignedResponseAlg);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                testPostDelegate(responseMessage, registerDetails, ssa);
            }
        }

        private delegate void TestPUTDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);

        private async Task TestPUT(
            ResponseType responseType,
            string[] grantTypes,
            string? authorizationSignedResponseAlg,
            string[]? redirectUrisForPut = null,
            bool expiredAccessToken = false,
            TestPUTDelegate? testPutDelegate = null,
            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grantTypes,
                authorizationSignedResponseAlg: authorizationSignedResponseAlg,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg);
            var dcrResponseMessage = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

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
                idTokenSignedResponseAlg: idTokenSignedResponseAlg);

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
            ResponseType responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            bool expiredAccessToken = false,
            TestDELETEDelegate? testDeleteDelegate = null,
            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grant_types,
                authorizationSignedResponseAlg: authorization_signed_response_alg,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg);

            var dcrResponseMessage = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

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
            ResponseType responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            bool expiredAccessToken = false,
            TestGETDelegate? testGetDelegate = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(Constants.SoftwareProducts.SoftwareProductId);

            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var dcrResponseMessage = await RegisterSoftwareProduct(
                responseType,
                grant_types,
                authorization_signed_response_alg,
                ssa,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg);

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
            ResponseType responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            string ssa,
            string? authorization_encrypted_response_alg = null,
            string? authorization_encrypted_response_enc = null,
            string? idTokenSignedResponseAlg = null)
        {
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grantTypes: grant_types,
                authorizationSignedResponseAlg: authorization_signed_response_alg,
                authorizationEncryptedResponseAlg: authorization_encrypted_response_alg,
                authorizationEncryptedResponseEnc: authorization_encrypted_response_enc,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg);

            var responseMessage = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            return responseMessage;
        }

        private async Task<DcrResponse> AssertDCR(
            ResponseType responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            HttpResponseMessage responseMessage,
            dynamic registerDetails,
            string ssa,
            string? authorizationEncryptedResponseAlg = null,
            string? authorizationEncryptedResponseEnc = null,
            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? expected_authorization_signed_response_alg = null)
        {
            var json = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<DcrResponse>(json) ?? throw new ArgumentNullException(nameof(responseMessage));
            var expectedResponseTypes = responseType.ToEnumMemberAttrValue();
            _ = response.response_types.Should().BeEquivalentTo(expectedResponseTypes);

            _ = response.grant_types.Should().BeEquivalentTo(grant_types);

            if (response.response_types?.Contains("code") ?? false)
            {
                if (authorization_signed_response_alg != null || expected_authorization_signed_response_alg != null)
                {
                    _ = response.authorization_signed_response_alg.Should().Be(expected_authorization_signed_response_alg ?? authorization_signed_response_alg);
                }

                if (_authServerOptions.JARM_ENCRYPTION_ON)
                {
                    if (authorizationEncryptedResponseAlg != null)
                    {
                        _ = response.authorization_encrypted_response_alg.Should().Be(authorizationEncryptedResponseAlg);
                    }

                    if (authorizationEncryptedResponseEnc != null)
                    {
                        _ = response.authorization_encrypted_response_enc.Should().Be(authorizationEncryptedResponseEnc);
                    }
                }
                else
                {
                    _ = response.authorization_encrypted_response_alg.Should().BeNull();
                    _ = response.authorization_encrypted_response_enc.Should().BeNull();
                }
            }
            else
            {
                _ = response.authorization_signed_response_alg.Should().BeNull();
                _ = response.authorization_encrypted_response_alg.Should().BeNull();
                _ = response.authorization_encrypted_response_enc.Should().BeNull();
            }

            _ = response.client_id.Should().NotBeNullOrEmpty();
            _ = response.client_id_issued_at.Should().NotBeNullOrEmpty();
            response.client_name.Should().Be(registerDetails.SoftwareProductName);
            response.client_description.Should().Be(registerDetails.SoftwareProductDescription);
            response.client_uri.Should().Be(registerDetails.ClientUri);

            response.org_id.Should().Be(registerDetails.BrandId.ToString());
            response.org_name.Should().Be(registerDetails.BrandName);

            _ = response.redirect_uris.Should().BeEquivalentTo([registerDetails.RedirectUris]);

            response.logo_uri.Should().Be(registerDetails.LogoUri);
            response.tos_uri.Should().Be(registerDetails.TosUri);
            response.policy_uri.Should().Be(registerDetails.PolicyUri);
            response.jwks_uri.Should().Be(registerDetails.JwksUri);
            response.revocation_uri.Should().Be(registerDetails.RevocationUri);

            response.sector_identifier_uri.Should().Be(registerDetails.SectorIdentifierUri);

            response.recipient_base_uri.Should().Be(registerDetails.RecipientBaseUri);

            _ = response.token_endpoint_auth_method.Should().Be("private_key_jwt");
            _ = response.token_endpoint_auth_signing_alg.Should().Be("PS256");

            _ = response.application_type.Should().Be("web");

            _ = response.id_token_signed_response_alg.Should().Be(idTokenSignedResponseAlg);

            _ = response.request_object_signing_alg.Should().Be("PS256");

            _ = response.software_statement.Should().Be(ssa);
            _ = response.software_id.Should().Be(Constants.SoftwareProducts.SoftwareProductId);
            _ = response.software_roles.Should().Be("data-recipient-software-product");

            _ = response.scope.Should().ContainAll("openid", "bank:accounts.basic:read", "cdr:registration");

            return response;
        }

        private static async Task<bool> AssertExpectedResponseCode(HttpResponseMessage responseMessage, HttpStatusCode expectedStatusCode)
        {
            _ = responseMessage.StatusCode.Should().Be(expectedStatusCode);
            if (responseMessage.StatusCode == expectedStatusCode)
            {
                return true;
            }

            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            _ = responseContent.Should().NotBe(responseContent);
            return false;
        }

        private async Task TestEncryption(
          ResponseType responseType,
          string grantTypes,
          string? authorizationSignedResponseAlg = null,
          string? authorizationEncryptedResponseAlg = null,
          string? authorizationEncryptedResponseEnc = null,
          string? idTokenSignedResponseAlg = null)
        {
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                authorization_encrypted_response_alg: authorizationEncryptedResponseAlg,
                authorization_encrypted_response_enc: authorizationEncryptedResponseEnc,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
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
                        authorizationEncryptedResponseAlg,
                        authorizationEncryptedResponseEnc,
                        idTokenSignedResponseAlg: idTokenSignedResponseAlg);
                });
        }
    }
}
