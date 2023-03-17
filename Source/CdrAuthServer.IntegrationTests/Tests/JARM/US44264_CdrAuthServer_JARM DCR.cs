// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CdrAuthServer.IntegrationTests.Fixtures;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Xunit;
using Dapper;
using System;
using System.Linq;
using System.Text;
using System.Net.Http.Headers;

namespace CdrAuthServer.IntegrationTests.JARM
{
    // JARM - DCR related tests
       public class US44264_CdrAuthServer_JARM_DCR : BaseTest, IClassFixture<TestFixture>
    {
        /// <summary>
        /// return an IdTokenEncryptedResponseAlg if response type contains "id_token", otherwise return null
        /// </summary>
        static string? idTokenEncryptedResponseAlg(string responseType)
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
        static string? idTokenEncryptedResponseEnc(string responseType)
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

        public class DCRResponse
        {
            public string? iss { get; set; }
            public string? iat { get; set; }
            public string? exp { get; set; }
            public string? jti { get; set; }
            public string? aud { get; set; }
            public string? client_id { get; set; }
            public string? client_id_issued_at { get; set; }
            public string? client_name { get; set; }
            public string? client_description { get; set; }
            public string? client_uri { get; set; }
            public string? org_id { get; set; }
            public string? org_name { get; set; }
            public string[]? redirect_uris { get; set; }
            public string? logo_uri { get; set; }
            public string? tos_uri { get; set; }
            public string? policy_uri { get; set; }
            public string? jwks_uri { get; set; }
            public string? revocation_uri { get; set; }
            public string? sector_identifier_uri { get; set; }
            public string? recipient_base_uri { get; set; }
            public string? token_endpoint_auth_method { get; set; }
            public string? token_endpoint_auth_signing_alg { get; set; }
            public string[]? grant_types { get; set; }
            public string[]? response_types { get; set; }
            public string? application_type { get; set; }
            public string? id_token_signed_response_alg { get; set; }
            public string? id_token_encrypted_response_alg { get; set; }
            public string? id_token_encrypted_response_enc { get; set; }
            public string? request_object_signing_alg { get; set; }
            public string? software_statement { get; set; }
            public string? software_id { get; set; }
            public string? software_roles { get; set; }
            public string? scope { get; set; }
            public string? authorization_signed_response_alg { get; set; }
            public string? authorization_encrypted_response_alg { get; set; }
            public string? authorization_encrypted_response_enc { get; set; }
        }

        // Lookup details for software product from Register
        static private dynamic LookupRegisterDetails(string softwareProductId)
        {
            using var connection = new SqlConnection(REGISTER_CONNECTIONSTRING);
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

        public delegate void TestPOSTDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        public async Task TestPOST(
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
            var registerDetails = LookupRegisterDetails(SOFTWAREPRODUCT_ID);

            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

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
            using (new AssertionScope())
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                testPostDelegate(responseMessage, registerDetails, ssa);
            }
        }

        public delegate void TestPUTDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        public async Task TestPUT(
            string responseType,
            string[] grant_types,
            string? authorization_signed_response_alg,
            string[]? redirectUrisForPut = null,
            bool expiredAccessToken = false,
            TestPUTDelegate? testPutDelegate = null,

            string? idTokenSignedResponseAlg = IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
            string? idTokenEncryptedResponseAlg = null,
            string? idTokenEncryptedResponseEnc = null)
        {
            // Arrange 
            var registerDetails = LookupRegisterDetails(SOFTWAREPRODUCT_ID);

            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

            var _registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grant_types: grant_types,
                authorization_signed_response_alg: authorization_signed_response_alg,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);
            var dcrResponseMessage = await DataHolder_Register_API.RegisterSoftwareProduct(_registrationRequest);

            if (dcrResponseMessage.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Expected Created but {dcrResponseMessage.StatusCode} - {await dcrResponseMessage.Content.ReadAsStringAsync()}");
            }
            DCRResponse dcrResponse = JsonConvert.DeserializeObject<DCRResponse>(await dcrResponseMessage.Content.ReadAsStringAsync());

            // Act 
            var registrationRequestForPut = DataHolder_Register_API.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grant_types: grant_types,
                authorization_signed_response_alg: authorization_signed_response_alg,
                redirect_uris: redirectUrisForPut,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);
            var accessToken = await new DataHolderAccessToken(dcrResponse.client_id).GetAccessToken(expiredAccessToken);
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{dcrResponse.client_id}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = accessToken,
                Content = new StringContent(registrationRequestForPut, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var responseMessage = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                testPutDelegate(responseMessage, registerDetails, ssa);
            }
        }

        public delegate void TestDELETEDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        public async Task TestDELETE(
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
            var registerDetails = LookupRegisterDetails(SOFTWAREPRODUCT_ID);

            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

            var _registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grant_types: grant_types,
                authorization_signed_response_alg: authorization_signed_response_alg,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);

            var dcrResponseMessage = await DataHolder_Register_API.RegisterSoftwareProduct(_registrationRequest);

            if (dcrResponseMessage.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Expected Created but {dcrResponseMessage.StatusCode} - {await dcrResponseMessage.Content.ReadAsStringAsync()}");
            }
            DCRResponse dcrResponse = JsonConvert.DeserializeObject<DCRResponse>(await dcrResponseMessage.Content.ReadAsStringAsync());

            // Act 
            var accessToken = await new DataHolderAccessToken(dcrResponse.client_id).GetAccessToken(expiredAccessToken);
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{dcrResponse.client_id}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Delete,
                AccessToken = accessToken,
            };
            var responseMessage = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                testDeleteDelegate(responseMessage, registerDetails, ssa);
            }
        }

        public delegate void TestGETDelegate(HttpResponseMessage responseMessage, dynamic registerDetails, string ssa);
        public async Task TestGET(
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
            var registerDetails = LookupRegisterDetails(SOFTWAREPRODUCT_ID);

            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

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

            DCRResponse dcrResponse = JsonConvert.DeserializeObject<DCRResponse>(await dcrResponseMessage.Content.ReadAsStringAsync());

            var accessToken = await new DataHolderAccessToken(dcrResponse.client_id).GetAccessToken(expiredAccessToken);

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{dcrResponse.client_id}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var responseMessage = await api.SendAsync(AllowAutoRedirect: false);

            // Assert
            using (new AssertionScope())
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                testGetDelegate(responseMessage, registerDetails, ssa);
            }
        }

        private static async Task<HttpResponseMessage> RegisterSoftwareProduct(
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
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                ssa,
                responseType: responseType,
                grant_types: grant_types,
                authorization_signed_response_alg: authorization_signed_response_alg,
                authorization_encrypted_response_alg: authorization_encrypted_response_alg,
                authorization_encrypted_response_enc: authorization_encrypted_response_enc,
                idTokenSignedResponseAlg: idTokenSignedResponseAlg,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg,
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc);

            var responseMessage = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            return responseMessage;
        }

        private async Task<DCRResponse> AssertDCR(
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
            var response = JsonConvert.DeserializeObject<DCRResponse>(json);

            string[] expectedResponseTypes = responseType.Contains(",") ? responseType.Split(",") : new string[] { responseType };
            response.response_types.Should().BeEquivalentTo(expectedResponseTypes);

            response.grant_types.Should().BeEquivalentTo(grant_types);

            if (response.response_types.Contains("code"))
            {
                if (authorization_signed_response_alg != null || expected_authorization_signed_response_alg != null)
                {
                    response.authorization_signed_response_alg.Should().Be(expected_authorization_signed_response_alg ?? authorization_signed_response_alg);
                }

                if (JARM_ENCRYPTION_ON)
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
            response.software_id.Should().Be(SOFTWAREPRODUCT_ID);
            response.software_roles.Should().Be("data-recipient-software-product");

            response.scope.Should().ContainAll("openid", "bank:accounts.basic:read", "cdr:registration");

            return response;
        }

        private async Task AssertError(HttpResponseMessage responseMessage, HttpStatusCode expectedStatusCode, string expectedResponse)
        {
            responseMessage.StatusCode.Should().Be(expectedStatusCode);

            Assert_HasContentType_ApplicationJson(responseMessage.Content);

            var json = await responseMessage.Content.ReadAsStringAsync();

            await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
        }

        async Task<bool> AssertExpectedResponseCode(HttpResponseMessage responseMessage, HttpStatusCode expectedStatusCode)
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

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        public async Task AC01_MDHJWKS_HF_AC1_POST_With_ShouldRespondWith_201Created(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','), // MJS - CDRAuthServer (see log) fails with:-  validation failed: [{"MemberNames": ["GrantTypes"], "ErrorMessage": "The 'grant_types' claim value must contain the 'authorization_code' value.", "$type": "ValidationResult"}]
                authorization_signed_response_alg: null,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "ES256")]
        public async Task AC02_MDHJWKS_ACF_AC20_POST_With_AuthSigningAlgES256_ShouldRespondWith_201Created(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC03_MDHJWKS_ACF_AC21_POST_With_AuthSigningAlgPS256_ShouldRespondWith_201Created(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, NULL)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "")]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "none")]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "RS256")]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "Foo")]
        public async Task AC05_MDHJWKS_ACF_AC24_POST_With_InvalidAuthSigningAlg_ShouldRespondWith_400BadRequest(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_client_metadata"",
                         ""error_description"": ""The 'authorization_signed_response_alg' claim value must be one of 'PS256,ES256'.""
                    }}";

                    if (string.IsNullOrEmpty(authorizationSignedResponseAlg) || authorizationSignedResponseAlg == NULL)
                    {
                        expectedResponse = @$"{{
                        ""error"": ""invalid_client_metadata"",
                         ""error_description"": ""The 'authorization_signed_response_alg' claim is missing.""
                    }}";
                    }

                    await AssertError(responseMessage, HttpStatusCode.BadRequest, expectedResponse);
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC06_MDHJWKS_AC2_POST_With_SoftwareIDAlreadyRegistered_ShouldRespondWith_400BadRequest(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            // Arrange
            var registerDetails = LookupRegisterDetails(SOFTWAREPRODUCT_ID);

            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

            // register software product
            HttpResponseMessage responseMessageSetup = await RegisterSoftwareProduct(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                ssa,
                idTokenSignedResponseAlg: IDTOKEN_SIGNED_RESPONSE_ALG_PS256,
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType));

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
                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType));

            // Assert
            using (new AssertionScope())
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                responseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                Assert_HasContentType_ApplicationJson(responseMessage.Content);

                var expectedResponse = @$"{{
                        ""error"": ""invalid_client_metadata"",
                         ""error_description"": ""ERR-DCR-001: Duplicate registrations for a given software_id are not valid.""
                    }}";

                await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
            }
        }

        // [Fact]
        public Task AC07_MDHJWKS_AC3_POST_With_UnapprovedSSA_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public Task AC08_MDHJWKS_AC6_POST_With_InvalidSSAPayload_RedirectURI_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();  // MJS - SSA is signed by Register, would need to create invalid SSA and sign with Register cert?
        }

        // [Fact] 
        public Task AC09_MDHJWKS_AC7_POST_With_InvalidSSAPayload_TokenEndpointAuthSigningALG_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();  // MJS - SSA is signed by Register, would need to create invalid SSA and sign with Register cert?
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        public async Task AC09_MDHJWKS_AC8_GET_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testGetDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType));
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC10_MDHJWKS_AC28_GET_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testGetDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC11_MDHJWKS_AC9_GET_With_ExpiredBearerToken_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestGET(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testGetDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        startsWith: true);
                });
        }

        // [Theory] 
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public Task AC12_MDHJWKS_AC10_GET_With_ClientIdNotRegistered_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        public async Task AC13_MDHJWKS_HF_AC11_PUT_With_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPUT(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPutDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC14_MDHJWKS_ACF_AC31_PUT_With_ShouldRespondWith_200OK(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPUT(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPutDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.OK))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }


        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC15_MDHJWKS_AC12_PUT_With_InvalidRegistrationProperty_RedirectUri_ShouldRespondWith_400BadRequest(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPUT(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                redirectUrisForPut: new string[] { "foo" },  // we are testing if invalid URI for PUT fails 

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPutDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    var expectedResponse = @$"{{
                        ""error"": ""invalid_redirect_uri"",
                         ""error_description"": ""ERR-DCR-003: The redirect_uri 'foo' is not valid as it is not included in the software_statement""
                    }}";

                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                });
        }


        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC16_MDHJWKS_AC13_PUT_With_ExpiredBearerToken_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestPUT(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPutDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        true);
                });
        }

        // [Theory] 
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public Task AC17_MDHJWKS_AC14_PUT_With_ClientIDNotRegistered_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            throw new NotImplementedException("No AC");
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC18_MDHJWKS_AC15_DELETE_ShouldRespondWith_204NoContent(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestDELETE(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testDeleteDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.NoContent))
                    {
                        return;
                    }

                    await Assert_HasNoContent(responseMessage.Content);
                });
        }

        [Theory]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, HYBRIDFLOW_GRANTTYPES, null)]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, AUTHORIZATIONCODEFLOW_GRANTTYPES, "PS256")]
        public async Task AC19_MDHJWKS_AC17_DELETE_WithExpiredBearerToken_ShouldRespondWith_401Unauthorized(string responseType, string grantTypes, string authorizationSignedResponseAlg)
        {
            await TestDELETE(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: authorizationSignedResponseAlg,
                expiredAccessToken: true,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testDeleteDelegate: (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at ",
                        responseMessage.Headers,
                        "WWW-Authenticate",
                        startsWith: true);
                });
        }

 
        public async Task TestEncryption(
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

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);
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
        public async Task AC21_MDHJWKS_ACF_AC33_Reject_POST_to_register_endpoint_Invalid_authorization_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public async Task AC22_MDHJWKS_ACF_AC34_Reject_POST_to_register_endpoint_Invalid_authorization_encrypted_response_enc()
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
        public async Task AC24_MDHJWKS_ACF_AC36_Reject_POST_to_register_endpoint_Omit_authorization_encrypted_response_alg_when_enc_is_provided()
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
        public async Task AC28_MDHJWKS_ACF_AC40_Reject_ACF_DCR_invalid_id_token_signed_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public async Task AC29_MDHJWKS_ACF_AC41_Reject_ACF_DCR_invalid_id_token_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public async Task AC30_MDHJWKS_ACF_AC42_Reject_ACF_DCR_invalid_id_token_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public async Task AC31_MDHJWKS_HF_AC43_Reject_HF_only_DCR_without_id_token_encryption()
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
        public async Task AC34_MDHJWKS_HF_AC46_Reject_HF_DCR_invalid_id_token_signed_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public async Task AC35_MDHJWKS_HF_AC47_Reject_HF_DCR_invalid_id_token_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact]   
        public async Task AC36_MDHJWKS_HF_AC48_Reject_HF_DCR_invalid_id_token_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        // [Fact] 
        public async Task AC37_MDHJWKS_ACF_HF_AC49_Reject_ACF_HF_DCR_without_id_token_encryption()
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
        public async Task AC40_MDHJWKS_ACF_HF_AC52_Reject_ACF_HF_DCR_invalid_id_token_signed_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact]
        public async Task AC41_MDHJWKS_ACF_HF_AC53_Reject_ACF_HF_DCR_invalid_id_token_encrypted_response_alg()
        {
            throw new NotImplementedException();
        }

        // [Fact]
        public async Task AC42_MDHJWKS_ACF_HF_AC54_Reject_ACF_HF_DCR_invalid_id_token_encrypted_response_enc()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData(AUTHORIZATIONCODEFLOW_RESPONSETYPE, "foo")]
        [InlineData(HYBRIDFLOW_RESPONSETYPE, "foo")]
        public async Task AC43_MDHJWKS_ACF_HF_AC55_With_InvalidGrantType_ShouldRespondWith_400BadRequest(string responseType, string grantTypes)
        {
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: null,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    responseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    var expectedResponse = @$"{{
                        ""error"": ""invalid_client_metadata"",
                         ""error_description"": ""The 'grant_types' claim value must contain the 'authorization_code' value.""
                    }}";

                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
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
            await TestPOST(
                responseType: responseType,
                grant_types: grantTypes.Split(','),
                authorization_signed_response_alg: null,

                idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType),
                idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType),

                testPostDelegate: async (responseMessage, registerDetails, ssa) =>
                {
                    if (!await AssertExpectedResponseCode(responseMessage, HttpStatusCode.Created))
                    {
                        return;
                    }

                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    await AssertDCR(
                        responseType,
                        grantTypes.Split(','),
                        authorizationSignedResponseAlg,
                        responseMessage,
                        registerDetails,
                        ssa,
                        expected_authorization_signed_response_alg: expected_authorization_signed_response_alg,
                        idTokenEncryptedResponseAlg: idTokenEncryptedResponseAlg(responseType) ?? "",   // returns "" for idTokenEncryptedResponseAlg
                        idTokenEncryptedResponseEnc: idTokenEncryptedResponseEnc(responseType) ?? "");  // returns "" for idTokenEncryptedResponseEnc
                });
        }
    }
}
