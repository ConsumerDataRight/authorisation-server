#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

namespace CdrAuthServer.IntegrationTests.Models
{
    public class OpenIdConfiguration
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

        public string? tls_client_certificate_bound_access_tokens { get; set; }

        public string[]? acr_values_supported { get; set; }
    }
}
