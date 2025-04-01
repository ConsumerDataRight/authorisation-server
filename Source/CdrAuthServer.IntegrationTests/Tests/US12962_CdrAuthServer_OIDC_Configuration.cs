#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using CdrAuthServer.IntegrationTests.Models;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CdrAuthServer.IntegrationTests
{
    public partial class US12962_CdrAuthServer_OIDC_Configuration : BaseTest
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US12962_CdrAuthServer_OIDC_Configuration(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            Log.Information("Constructing {ClassName}.", nameof(US12962_CdrAuthServer_OIDC_Configuration));

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _apiServiceDirector = apiServiceDirector ?? throw new System.ArgumentNullException(nameof(apiServiceDirector));
        }

        [Fact]
        public async Task AC01_FromMDH_Get_ShouldRespondWith_200OK_OIDC_Configuration()
        {
            // Act
            var response = await _apiServiceDirector.BuildAuthServerOpenIdConfigurationAPI().SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check json
                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<OpenIdConfiguration>(actualJson);

                actual.issuer.Should().Be(_authServerOptions.CDRAUTHSERVER_BASEURI);
                actual.authorization_endpoint.Should().Be($"{_authServerOptions.CDRAUTHSERVER_BASEURI}/connect/authorize");
                actual.token_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/token");
                actual.introspection_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/introspect");
                actual.userinfo_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/userinfo");
                actual.registration_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/register");
                actual.jwks_uri.Should().Be($"{_authServerOptions.CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration/jwks");
                actual.pushed_authorization_request_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/par");
                actual.revocation_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/revocation");
                actual.cdr_arrangement_revocation_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/arrangements/revoke");
                actual.acr_values_supported.Should().IntersectWith(new[] { "urn:cds.au:cdr:2", "urn:cds.au:cdr:3" });
                actual.tls_client_certificate_bound_access_tokens.Should().Be("true");
                actual.id_token_signing_alg_values_supported.Should().BeEquivalentTo(["ES256", "PS256"]);
                actual.token_endpoint_auth_signing_alg_values_supported.Should().BeEquivalentTo(["ES256", "PS256"]);
                actual.token_endpoint_auth_methods_supported.Should().BeEquivalentTo(["private_key_jwt"]);
                actual.subject_types_supported.Should().BeEquivalentTo(["pairwise"]);
                actual.grant_types_supported.Should().BeEquivalentTo(["authorization_code", "client_credentials", "refresh_token"]);
                actual.scopes_supported.Should().Contain(new[] { "openid", "profile", "cdr:registration", "bank:accounts.basic:read", "bank:transactions:read", "common:customer.basic:read", });
                actual.claims_supported.Should().Contain(new[] { "name", "given_name", "family_name", "sharing_duration", "iss", "sub", "aud", "acr", "exp", "iat", "nonce", "auth_time", "updated_at" });
                actual.response_types_supported.Should().Contain(new[] { "code" });
                actual.response_modes_supported.Should().BeEquivalentTo(new[] { "jwt" });
            }
        }

        [Fact]
        public async Task AC01_Get_ShouldRespondWith_200OK_OIDC_Configuration()
        {
            // Act
            var response = await _apiServiceDirector.BuildAuthServerOpenIdConfigurationAPI().SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(response.Content);

                // Assert - Check json
                var expected = $@"
                    {{
                    ""issuer"": ""{_authServerOptions.CDRAUTHSERVER_BASEURI}"",
                    ""jwks_uri"": ""{_authServerOptions.CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration/jwks"",
                    ""registration_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/register"",
                    ""authorization_endpoint"": ""{_authServerOptions.CDRAUTHSERVER_BASEURI}/connect/authorize"",
                    ""pushed_authorization_request_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/par"",
                    ""token_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/token"",
                    ""userinfo_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/userinfo"",
                    ""introspection_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/introspect"",
                    ""cdr_arrangement_revocation_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/arrangements/revoke"",
                    ""revocation_endpoint"": ""{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/revocation"",
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
                        ""jwt""
                    ],
                    ""response_types_supported"": [
                        ""code""
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
                    ""authorization_signing_alg_values_supported"": [
                        ""PS256"",
                        ""ES256""
                    ]";

                if (_authServerOptions.JARM_ENCRYPTION_ON)
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

                await Assertions.AssertHasContentJson(expected, response.Content);

#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                await WriteJsonToFileAsync($"c:/temp/expected.json", expected);
                await WriteJsonToFileAsync($"c:/temp/actual.json", response.Content);
#endif
            }
        }
    }
}
