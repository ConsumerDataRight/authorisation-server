using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using CdrAuthServer.IntegrationTests.Infrastructure;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using CdrAuthServer.IntegrationTests.Fixtures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15584_CdrAuthServer_Registration_POST : BaseTest, IClassFixture<TestFixture>
    {
        // Get payload from a JWT. Need to convert JArray claims into a string[] otherwise when we re-sign the token the JArray is not properly serialized.
        static private Dictionary<string, object> GetJWTPayload(JwtSecurityToken jwt)
        {
            var payload = new Dictionary<string, object>();

            foreach (var kvp in jwt.Payload)
            {
                // Need to process JArray as shown below because Microsoft.IdentityModel.Json.Linq.JArray is protected.
                if (kvp.Value.GetType().Name == "JArray")
                {
                    var list = new List<string>();

                    foreach (var item in kvp.Value as IEnumerable ?? throw new NullReferenceException())
                    {
                        list.Add(item.ToString() ?? throw new NullReferenceException());
                    }

                    payload.Add(kvp.Key, list.ToArray());
                }
                else
                {
                    payload.Add(kvp.Key, kvp.Value);
                }
            }

            return payload;
        }

        [Theory]
        [InlineData("3")]
        public async Task AC01_Post_WithUnregistedSoftwareProduct_ShouldRespondWith_201Created_CreatedProfile(string ssaVersion)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, ssaVersion);

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                response.StatusCode.Should().Be(
                    HttpStatusCode.Created,
                    $"NB: response.Content is {responseContent}");

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Check for the registration response properties.
                    var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    Assert.True(json["redirect_uris"] is JArray && (json["redirect_uris"] as JArray).Any());
                    Assert.True(json["grant_types"] is JArray && (json["grant_types"] as JArray).Any());
                    Assert.True(json["response_types"] is JArray && (json["response_types"] as JArray).Any());
                    Assert.Equal("web", json["application_type"]);
                    Assert.Equal("data-recipient-software-product", json["software_roles"]);
                    Assert.Equal("PS256", json["token_endpoint_auth_signing_alg"]);
                    Assert.Equal("private_key_jwt", json["token_endpoint_auth_method"]);
                    Assert.Equal("PS256", json["id_token_signed_response_alg"]);
                    Assert.Equal("RSA-OAEP", json["id_token_encrypted_response_alg"]);
                    Assert.Equal("A256GCM", json["id_token_encrypted_response_enc"]);
                    Assert.Equal("PS256", json["request_object_signing_alg"]);
                    Assert.Equal("openid profile common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:regular_payments:read bank:payees:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.der:read energy:electricity.usage:read cdr:registration", json["scope"]);
                    Assert.Equal(ssa, json["software_statement"]);
                }
            }
        }

        [Theory]
        [InlineData("3")]
        public async Task AC01_Post_WithUnregistedSoftwareProduct_MandatoryOnlyFields_ShouldRespondWith_201Created_CreatedProfile(string ssaVersion)
        {
            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer();
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, ssaVersion);

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                ssa,
                applicationType: "",
                requestObjectSigningAlg: "",
                redirect_uris: null);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                response.StatusCode.Should().Be(
                    HttpStatusCode.Created,
                    $"NB: response.Content is {responseContent}");

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Check for the registration response properties.
                    var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    Assert.True(json["redirect_uris"] is JArray && (json["redirect_uris"] as JArray).Any());
                    Assert.True(json["grant_types"] is JArray && (json["grant_types"] as JArray).Any());
                    Assert.True(json["response_types"] is JArray && (json["response_types"] as JArray).Any());
                    Assert.Equal("web", json["application_type"]);
                    Assert.Equal("data-recipient-software-product", json["software_roles"]);
                    Assert.Equal("PS256", json["token_endpoint_auth_signing_alg"]); 
                    Assert.Equal("private_key_jwt", json["token_endpoint_auth_method"]);
                    Assert.Equal("PS256", json["id_token_signed_response_alg"]);
                    Assert.Equal("RSA-OAEP", json["id_token_encrypted_response_alg"]);
                    Assert.Equal("A256GCM", json["id_token_encrypted_response_enc"]);
                    Assert.Equal("PS256", json["request_object_signing_alg"]);
                    Assert.Equal("openid profile common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:regular_payments:read bank:payees:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.der:read energy:electricity.usage:read cdr:registration", json["scope"]);
                    Assert.Equal(ssa, json["software_statement"]);
                }
            }
        }

        [Fact]
        public async Task AC02_Post_WithRegistedSoftwareProduct_ShouldRespondWith_400BadRequest_DuplicateErrorResponse()
        {
            static async Task Arrange(string ssa)
            {
                TestSetup.DataHolder_PurgeIdentityServer();

                var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
                var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception("Unable to register software product");
                }
            }

            // Arrange
            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");
            await Arrange(ssa);

            // Act - Try to register the same product again
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @"{
                    ""error"": ""invalid_client_metadata"",
                    ""error_description"": ""ERR-DCR-001: Duplicate registrations for a given software_id are not valid.""
                }";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.Created)]
        [InlineData(false, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithValidButUnapprovedSSA_400BadRequest_UnapprovedSSAErrorResponse(bool signedWithRegisterCertificate, HttpStatusCode expectedStatusCode)
        {
            // Fake a SSA by signing with certificate that is not the Register certificate
            static string CreateFakeSSA(string ssa)
            {
                var decodedSSA = new JwtSecurityTokenHandler().ReadJwtToken(ssa);

                var payload = GetJWTPayload(decodedSSA);

                // Sign with a non-SSA certicate (ie just use a data recipient certificate)
                var fakeSSA = JWT2.CreateJWT(
                    CERTIFICATE_FILENAME,
                    CERTIFICATE_PASSWORD,
                    payload);

                return fakeSSA;
            }

            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");

            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                signedWithRegisterCertificate ? ssa : CreateFakeSSA(ssa)
            );

            // Act
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check json
                    var expectedResponse = @"{
                        ""error"": ""invalid_software_statement"",
                        ""error_description"": ""ERR-DCR-005: SSA validation failed.""
                    }";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC06_Post_WithInvalidSSAPayload_400BadRequest_InvalidSSAPayloadResponse()
        {
            // Arrange 
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");
            const string REDIRECT_URI = "foo";
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa, redirect_uris: new string[] { REDIRECT_URI });

            // Act
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                var expectedResponse = @$"{{
                        ""error"": ""invalid_redirect_uri"",
                         ""error_description"": ""ERR-DCR-003: The redirect_uri '{REDIRECT_URI}' is not valid as it is not included in the software_statement""
                }}";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        public async Task AC07_Post_WithInvalidMetadata_400BadRequest_InvalidMetadataResponse()
        {
            // Arrange 
            TestSetup.DataHolder_PurgeIdentityServer();

            var ssa = await Register_SSA_API.GetSSA(BRANDID, SOFTWAREPRODUCT_ID, "3");
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa, token_endpoint_auth_signing_alg: "HS256"); // HS256 is invalid metadata (ie should be PS256)

            // Act
            var response = await DataHolder_Register_API.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check application/json
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expectedResponse = @"{
                    ""error"":""invalid_client_metadata"",
                    ""error_description"":""The 'token_endpoint_auth_signing_alg' claim value must be one of 'PS256,ES256'.""
                }";

                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }
    }
}