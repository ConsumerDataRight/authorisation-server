#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;

namespace CdrAuthServer.IntegrationTests
{
    public class US12962_CdrAuthServer_OIDC_Configuration : BaseTest
    {
        private class AC01_Expected
        {
            public string? issuer { get; set; }
            public string? authorization_endpoint { get; set; }
            public string? jwks_uri { get; set; }
            public string? token_endpoint { get; set; }
            public string? introspection_endpoint { get; set; }
            public string? userinfo_endpoint { get; set; }
            public string? registration_endpoint { get; set; }
            public string? revocation_endpoint { get; set; }
            public string? cdr_arrangement_revocation_endpoint { get; set; }
            public string? pushed_authorization_request_endpoint { get; set; }
            public string[]? claims_supported { get; set; }
            public string[]? scopes_supported { get; set; }
            public string[]? response_types_supported { get; set; }
            public string[]? response_modes_supported { get; set; }
            public string[]? grant_types_supported { get; set; }
            public string[]? subject_types_supported { get; set; }
            public string[]? id_token_signing_alg_values_supported { get; set; }
            public string[]? token_endpoint_auth_signing_alg_values_supported { get; set; }
            public string[]? token_endpoint_auth_methods_supported { get; set; }
            public string[]? id_token_encryption_alg_values_supported { get; set; }
            public string[]? id_token_encryption_enc_values_supported { get; set; }
            public string? tls_client_certificate_bound_access_tokens { get; set; }
            public string[]? acr_values_supported { get; set; }
        }

        [Fact] 
        public async Task AC01_FromMDH_Get_ShouldRespondWith_200OK_OIDC_Configuration()
        {
            // Act
            var response = await new API
            {
                Method = HttpMethod.Get,
                URL = $"{CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration",
            }.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<AC01_Expected>(actualJson);
                actual.issuer.Should().Be(CDRAUTHSERVER_BASEURI);
                actual.authorization_endpoint.Should().Be($"{CDRAUTHSERVER_BASEURI}/connect/authorize");
                actual.token_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/token");
                actual.introspection_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/introspect");
                actual.userinfo_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/userinfo");
                actual.registration_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/register");
                actual.jwks_uri.Should().Be($"{CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration/jwks");
                actual.pushed_authorization_request_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/par");
                actual.revocation_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/revocation");
                actual.cdr_arrangement_revocation_endpoint.Should().Be($"{CDRAUTHSERVER_SECUREBASEURI}/connect/arrangements/revoke");
                actual.acr_values_supported.Should().IntersectWith(new[] { "urn:cds.au:cdr:2", "urn:cds.au:cdr:3" });
                actual.id_token_encryption_alg_values_supported.Should().IntersectWith(new[] { "RSA-OAEP", "RSA-OAEP-256" });
                actual.id_token_encryption_enc_values_supported.Should().IntersectWith(new[] { "A128CBC-HS256", "A256GCM" });
                actual.tls_client_certificate_bound_access_tokens.Should().Be("true");
                actual.id_token_signing_alg_values_supported.Should().BeEquivalentTo(new[] { "ES256", "PS256" });
                actual.token_endpoint_auth_signing_alg_values_supported.Should().BeEquivalentTo(new[] { "ES256", "PS256" });
                actual.token_endpoint_auth_methods_supported.Should().BeEquivalentTo(new[] { "private_key_jwt" });
                actual.subject_types_supported.Should().BeEquivalentTo(new[] { "pairwise" });
                actual.grant_types_supported.Should().BeEquivalentTo(new[] { "authorization_code", "client_credentials", "refresh_token" });               
                actual.scopes_supported.Should().Contain(new[] { "openid", "profile", "cdr:registration", "bank:accounts.basic:read", "bank:transactions:read", "common:customer.basic:read", });
                actual.claims_supported.Should().Contain(new[] { "name", "given_name", "family_name", "sharing_duration", "iss", "sub", "aud", "acr", "exp", "iat", "nonce", "auth_time", "updated_at" });
                actual.response_types_supported.Should().Contain(new[] { "code id_token" });
                actual.response_modes_supported.Should().Contain(new[] { "form_post", "fragment" });
            }
        }

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_OIDC_Configuration()
        {
            // Act
            var response = await new API
            {
                Method = HttpMethod.Get,
                URL = $"{CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration",
            }.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check json
                var expected = $@"
                    {{
                    ""issuer"": ""{CDRAUTHSERVER_BASEURI}"",
                    ""jwks_uri"": ""{CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration/jwks"",
                    ""registration_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/register"",
                    ""authorization_endpoint"": ""{CDRAUTHSERVER_BASEURI}/connect/authorize"",
                    ""pushed_authorization_request_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/par"",
                    ""token_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/token"",
                    ""userinfo_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/userinfo"",
                    ""introspection_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/introspect"",
                    ""cdr_arrangement_revocation_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/arrangements/revoke"",
                    ""revocation_endpoint"": ""{CDRAUTHSERVER_SECUREBASEURI}/connect/revocation"",
                    ""scopes_supported"": [
                        ""profile"",
                        ""openid"",
                        ""common:customer.basic:read"",
                        ""common:customer.detail:read"",
                        ""bank:accounts.basic:read"",
                        ""bank:accounts.detail:read"",
                        ""bank:transactions:read"",
                        ""bank:payees:read"",
                        ""bank:regular_payments:read"",
                        ""energy:electricity.servicepoints.basic:read"",
                        ""energy:electricity.servicepoints.detail:read"",
                        ""energy:electricity.usage:read"",
                        ""energy:electricity.der:read"",
                        ""energy:accounts.basic:read"",
                        ""energy:accounts.detail:read"",
                        ""energy:accounts.paymentschedule:read"",
                        ""energy:accounts.concessions:read"",
                        ""energy:billing:read"",
                        ""admin:metrics.basic:read"",
                        ""admin:metadata:update"",
                        ""cdr:registration""
                    ],
                    ""claims_supported"": [
                        ""name"",
                        ""given_name"",
                        ""family_name"",
                        ""sharing_duration"",
                        ""iss"",
                        ""sub"",
                        ""aud"",
                        ""acr"",
                        ""exp"",
                        ""iat"",
                        ""nonce"",
                        ""auth_time"",
                        ""updated_at""
                    ],
                    ""grant_types_supported"": [
                        ""authorization_code"",
                        ""refresh_token"",
                        ""client_credentials""
                    ],
                    ""subject_types_supported"": [
                        ""pairwise""
                    ],
                    ""response_modes_supported"": [
                        ""fragment"",
                        ""form_post"",
                        ""jwt""
                    ],
                    ""response_types_supported"": [
                        ""code"",
                        ""code id_token""
                    ],
                    ""code_challenge_methods_supported"": [
                        ""S256""
                    ],
                    ""require_pushed_authorization_requests"": true,
                    ""request_parameter_supported"": false,
                    ""request_uri_parameter_supported"": true,
                    ""request_object_signing_alg_values_supported"": [
                        ""PS256"",
                        ""ES256""
                    ],
                    ""tls_client_certificate_bound_access_tokens"": true,
                    ""claims_parameter_supported"": true,
                    ""acr_values_supported"": [
                        ""urn:cds.au:cdr:2""
                    ],
                    ""token_endpoint_auth_signing_alg_values_supported"": [
                        ""PS256"",
                        ""ES256""
                    ],
                    ""token_endpoint_auth_methods_supported"": [
                        ""private_key_jwt""
                    ],
                    ""id_token_signing_alg_values_supported"": [
                        ""PS256"",
                        ""ES256""
                    ],
                    ""id_token_encryption_alg_values_supported"": [
                        ""RSA-OAEP"",
                        ""RSA-OAEP-256""
                    ],
                    ""id_token_encryption_enc_values_supported"": [
                        ""A128CBC-HS256"",
                        ""A256GCM""
                    ],
                    ""authorization_signing_alg_values_supported"": [
                        ""PS256"",
                        ""ES256""
                    ]";

                if (JARM_ENCRYPTION_ON)
                {
                    expected += $@",
                        ""authorization_encryption_alg_values_supported"": [
                            ""RSA-OAEP"",
                            ""RSA-OAEP-256""
                        ],
                        ""authorization_encryption_enc_values_supported"": [
                            ""A128CBC-HS256"",
                            ""A256GCM""
                        ]";
                }

                expected += "}";

                await Assert_HasContent_Json(expected, response.Content);

#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                await WriteJsonToFileAsync($"c:/temp/expected.json", expected);
                await WriteJsonToFileAsync($"c:/temp/actual.json", response.Content);
#endif
            }
        }
    }
}
