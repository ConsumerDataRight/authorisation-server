// #define DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CdrAuthServer.IntegrationTests.Fixtures;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using System;

namespace CdrAuthServer.IntegrationTests.JARM
{
    // JARM - PAR related tests
    // https://cdr-internal.atlassian.net/wiki/spaces/PT/pages/47513616/MDH+PAR+endpoint+Acceptance+Criteria
    public class US44264_CdrAuthServer_JARM_PAR : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        public class PARResponse
        {
            public string? request_uri { get; set; }
            public string? expires_in { get; set; }
        }

        public delegate void TestPOSTDelegate(HttpResponseMessage responseMessage);
        public async Task TestPOST(
            string? responseType = null,
            string? grantType = null,
            string? responseMode = "fragment",
            string? authorization_signed_response_alg = null,
            string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
            string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
            string? clientAssertion = null,
            TestPOSTDelegate? testPostDelegate = null)
        {
            // Arrange 

            // Act
            var responseMessage = await DataHolder_Par_API.SendRequest(
                responseType: responseType,
                grant_type: grantType,
                responseMode: responseMode,
                authorization_signed_response_alg: authorization_signed_response_alg,
                clientId: clientId,
                clientAssertionType: clientAssertionType,
                clientAssertion: clientAssertion
            );

            // Assert
            using (new AssertionScope())
            {
#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await responseMessage.Content.ReadAsStringAsync());
#endif

                if (testPostDelegate != null)
                {
                    testPostDelegate(responseMessage);
                }
            }
        }

        private async Task AssertError(HttpResponseMessage responseMessage, HttpStatusCode expectedStatusCode, string expectedResponse)
        {
            responseMessage.StatusCode.Should().Be(expectedStatusCode);

            Assert_HasContentType_ApplicationJson(responseMessage.Content);

            var json = await responseMessage.Content.ReadAsStringAsync();

            await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
        }

        [Theory]
        [InlineData(US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_RESPONSETYPE, US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_GRANTTYPES, "fragment", null)]
        [InlineData(US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_RESPONSETYPE, US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_GRANTTYPES, "form_post", null)]
        [InlineData(US44264_CdrAuthServer_JARM_DCR.AUTHORIZATIONCODEFLOW_RESPONSETYPE, US44264_CdrAuthServer_JARM_DCR.AUTHORIZATIONCODEFLOW_GRANTTYPES, "jwt", "PS256")]
        public async Task AC01_MDHPAR_AC01_HappyPath_ShouldRespondWith_201Created(string responseType, string grantTypes, string responseMode, string authorizationSignedResponseAlg)
        {
            // Arrange

            // Act
            var response = await DataHolder_Par_API.SendRequest(
                responseType: responseType,
                responseMode: responseMode,
                grant_type: grantTypes,
                authorization_signed_response_alg: authorizationSignedResponseAlg
            );

            var responseText = await response.Content.ReadAsStringAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created, responseText);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
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
            await TestPOST(
                grantType: "client_credentials",
                clientId: "foo",
                clientAssertionType: "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_client"",
                         ""error_description"": ""ERR-GEN-004: Client not found""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });  
        }

        [Fact]
        public async Task AC06_MDHPAR_AC09_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest()
        {
            await TestPOST(
                grantType: "client_credentials",
                clientId: BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID),
                clientAssertionType: "foo",
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_client"",
                         ""error_description"": ""ERR-CLIENT_ASSERTION-003: client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });  
        }

        [Fact]
        public async Task AC07_MDHPAR_AC10_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest()
        {
            await TestPOST(
                grantType: "client_credentials",
                clientId: BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID),
                clientAssertionType: "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                clientAssertion: "foo",
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_client"",
                         ""error_description"": ""ERR-CLIENT_ASSERTION-005: Cannot read client_assertion.  Invalid format.""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });  
        }

        // [Fact]
        public async Task AC08_MDHPAR_AC11_WithClientAssertionAssociatedWithDifferentClientId_ShouldRespondWith_400BadRequest()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("query.jwt")]
        [InlineData("fragment.jwt")]
        [InlineData("form_post.jwt")]
        public async Task AC09_MDHPAR_AC12_HF_WithInvalidResponseMode_ShouldRespondWith_400BadRequest(string responseMode)
        {
            await TestPOST(
                responseType: US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_RESPONSETYPE,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_request"",
                         ""error_description"": ""ERR-GEN-013: response_mode is not supported""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });  
        }

        [Theory]
        [InlineData(null, "invalid_request", "ERR-GEN-013: response_mode is not supported")]
        [InlineData("Foo", "invalid_request", "ERR-GEN-013: response_mode is not supported")]
        [InlineData("query", "invalid_request", "ERR-GEN-013: response_mode is not supported")]
        [InlineData("fragment", "invalid_request_object", "ERR-GEN-026: Invalid response_mode for response_type")]
        [InlineData("form_post", "invalid_request_object", "ERR-GEN-026: Invalid response_mode for response_type")]
        [InlineData("query.jwt", "invalid_request", "ERR-GEN-013: response_mode is not supported")]
        [InlineData("form_post.jwt", "invalid_request", "ERR-GEN-013: response_mode is not supported")]
        [InlineData("fragment.jwt", "invalid_request", "ERR-GEN-013: response_mode is not supported")]
        public async Task AC10_MDHPAR_AC13_ACF_WithInvalidResponseMode_ShouldRespondWith_400BadRequest(string responseMode,
           string expectedErrorType, string expectedErrorDescription)
        {
            await TestPOST(
                responseType: US44264_CdrAuthServer_JARM_DCR.AUTHORIZATIONCODEFLOW_RESPONSETYPE,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""{expectedErrorType}"",
                         ""error_description"": ""{expectedErrorDescription}""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });  
        }
     
        [Theory]
        [InlineData("Foo")]
        [InlineData("token")]
        [InlineData("code token")]
        [InlineData("id_token token")]
        [InlineData("code id_token token")]
        [InlineData("code Foo")]
        [InlineData("code id_token Foo")]
        public async Task AC13_MDHPAR_AC15_WithInvalidResponseType_ShouldRespondWith_400BadRequest(string responseType)
        {
            await TestPOST(
                responseType: responseType,
                responseMode: "fragment",
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_request"",
                         ""error_description"": ""ERR-GEN-009: response_type is not supported""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });    
        }

        [Fact]
        public async Task AC15_MDHPAR_AC16_WithMissingResponseType_ShouldRespondWith_400BadRequest()
        {
            await TestPOST(
                responseType: null,
                responseMode: "fragment",
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_request"",
                         ""error_description"": ""ERR-GEN-008: response_type is missing""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });                
        }

        [Theory]
        [InlineData("jwt")]
        public async Task AC16_MDHPAR_AC17_HF_WithInvalidResponseMode_ShouldRespondWith_400BadRequest(string responseMode)
        {
            await TestPOST(
                responseType: US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_RESPONSETYPE,
                grantType: US44264_CdrAuthServer_JARM_DCR.HYBRIDFLOW_GRANTTYPES,
                responseMode: responseMode,
                testPostDelegate: async (response) =>
                {
                    var expectedResponse = @$"{{
                        ""error"": ""invalid_request_object"",
                         ""error_description"": ""ERR-GEN-026: Invalid response_mode for response_type""
                    }}";
                    await AssertError(response, HttpStatusCode.BadRequest, expectedResponse);
                });
        }
    }
}
