#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System.Net;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;
using Xunit.DependencyInjection;

namespace CdrAuthServer.IntegrationTests.JARM
{
    // JARM - OIDC  related tests
    public class US44264_CdrAuthServer_JARM_OIDC : BaseTest
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US44264_CdrAuthServer_JARM_OIDC(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        private class OIDCResponse
        {
            public string? issuer { get; set; }

            public string? jwks_uri { get; set; }

            public string? registration_endpoint { get; set; }

            public string? authorization_endpoint { get; set; }

            public string? token_endpoint { get; set; }

            public string? userinfo_endpoint { get; set; }

            public string? revocation_endpoint { get; set; }

            public string[]? scopes_supported { get; set; }

            public string[]? claims_supported { get; set; }

            public string[]? id_token_signing_alg_values_supported { get; set; }

            public string[]? subject_types_supported { get; set; }

            public string[]? code_challenge_methods_supported { get; set; }

            public bool request_parameter_supported { get; set; }

            public bool request_uri_parameter_supported { get; set; }

            public string? introspection_endpoint { get; set; }

            public string? pushed_authorization_request_endpoint { get; set; }

            public string? cdr_arrangement_revocation_endpoint { get; set; }

            public string[]? acr_values_supported { get; set; }

            public bool require_pushed_authorization_requests { get; set; }

            public string[]? request_object_signing_alg_values_supported { get; set; }

            public string[]? id_token_encryption_alg_values_supported { get; set; }

            public string[]? id_token_encryption_enc_values_supported { get; set; }

            public string[]? token_endpoint_auth_signing_alg_values_supported { get; set; }

            public string[]? response_types_supported { get; set; }

            public string[]? grant_types_supported { get; set; }

            public string[]? token_endpoint_auth_methods_supported { get; set; }

            public bool tls_client_certificate_bound_access_tokens { get; set; }

            public bool claims_parameter_supported { get; set; }

            public string[]? response_modes_supported { get; set; }

            public string[]? authorization_signing_alg_values_supported { get; set; }
        }

        private async Task TestOIDC(string[] expectedScopes)
        {
            // Act
            var responseMessage = await _apiServiceDirector.BuildAuthServerOpenIdConfigurationAPI().SendAsync();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assertions.AssertHasContentTypeApplicationJson(responseMessage.Content);

                // Assert - Check content
                var response = JsonConvert.DeserializeObject<OIDCResponse>(await responseMessage.Content.ReadAsStringAsync());
                response.issuer.Should().Be(_authServerOptions.CDRAUTHSERVER_BASEURI);
                response.jwks_uri.Should().Be($"{_authServerOptions.CDRAUTHSERVER_BASEURI}/.well-known/openid-configuration/jwks");
                response.registration_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/register");
                response.authorization_endpoint.Should().Be($"{_authServerOptions.CDRAUTHSERVER_BASEURI}/connect/authorize");
                response.token_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/token");
                response.userinfo_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/userinfo");
                response.revocation_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/revocation");
                response.scopes_supported.Should().Contain(expectedScopes);
                response.claims_supported.Should().Contain(new string[] { "name", "given_name", "family_name", "sharing_duration", "iss", "sub", "aud", "acr", "exp", "iat", "nonce", "auth_time", "updated_at" });
                response.id_token_signing_alg_values_supported.Should().Contain(new string[] { "PS256", "ES256" });
                response.subject_types_supported.Should().Contain(new string[] { "pairwise" });
                response.code_challenge_methods_supported.Should().Contain(new string[] { "S256" });
                response.request_parameter_supported.Should().Be(false);
                response.request_uri_parameter_supported.Should().Be(true);
                response.introspection_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/introspect");
                response.pushed_authorization_request_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/par");
                response.cdr_arrangement_revocation_endpoint.Should().Be($"{_options.CDRAUTHSERVER_SECUREBASEURI}/connect/arrangements/revoke");
                response.acr_values_supported.Should().BeSubsetOf(new string[] { "urn:cds.au:cdr:2", "urn:cds.au:cdr:3" });
                response.require_pushed_authorization_requests.Should().Be(true);
                response.request_object_signing_alg_values_supported.Should().Contain(new string[] { "PS256", "ES256" });
                response.token_endpoint_auth_signing_alg_values_supported.Should().Contain(new string[] { "PS256", "ES256" });
                response.response_types_supported.Should().Contain(new string[] { "code" });
                response.grant_types_supported.Should().Contain(new string[] { "authorization_code", "refresh_token", "client_credentials" });
                response.token_endpoint_auth_methods_supported.Should().Contain(new string[] { "private_key_jwt" });
                response.tls_client_certificate_bound_access_tokens.Should().Be(true);
                response.claims_parameter_supported.Should().Be(true);
                response.response_modes_supported.Should().BeEquivalentTo(["jwt"]);

                response.authorization_signing_alg_values_supported.Should().Contain(new string[] { "PS256", "ES256" });

#if DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON
                WriteJsonToFile($"c:/temp/actual.json", await response.Content.ReadAsStringAsync());
#endif
            }
        }

        [Fact]
        public async Task AC01_MDH_OIDC_AC01_Get_ShouldRespondWith_200OK_OIDC_Configuration()
        {
            await TestOIDC(["openid", "profile", "cdr:registration", "bank:accounts.basic:read", "bank:transactions:read", "common:customer.basic:read"]);
        }

        // https://cdr-internal.atlassian.net/wiki/spaces/PT/pages/44728338/MDHE+OIDC+Discovery+.well-known+Acceptance+Criteria
        [Fact]
        public async Task AC02_MDHE_OIDC_AC01_Get_ShouldRespondWith_200OK_OIDC_Configuration()
        {
            await TestOIDC(["openid", "profile", "cdr:registration", "energy:accounts.basic:read", "energy:accounts.concessions:read", "common:customer.basic:read"]);
        }
    }
}
