using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class Discovery
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonProperty("jwks_uri")]
        public string JwksUri { get; set; } = string.Empty;

        [JsonProperty("registration_endpoint")]
        public string RegistrationEndpoint { get; set; } = string.Empty;

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        [JsonProperty("pushed_authorization_request_endpoint")]
        public string PushedAuthorizationEndpoint { get; set; } = string.Empty;

        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [JsonProperty("userinfo_endpoint")]
        public string UserinfoEndpoint { get; set; } = string.Empty;

        [JsonProperty("introspection_endpoint")]
        public string IntrospectionEndpoint { get; set; } = string.Empty;

        [JsonProperty("cdr_arrangement_revocation_endpoint")]
        public string ArrangementRevocationEndpoint { get; set; } = string.Empty;

        [JsonProperty("revocation_endpoint")]
        public string RevocationEndpoint { get; set; } = string.Empty;

        [JsonProperty("scopes_supported")]
        public IList<string>? ScopesSupported { get; set; }

        [JsonProperty("claims_supported")]
        public IList<string>? ClaimsSupported { get; set; }

        [JsonProperty("grant_types_supported")]
        public IList<string>? GrantTypesSupported { get; set; }

        [JsonProperty("subject_types_supported")]
        public IList<string>? SubjectTypesSupported { get; set; }

        [JsonProperty("response_modes_supported")]
        public IList<string>? ResponseModesSupported { get; set; }

        [JsonProperty("response_types_supported")]
        public IList<string>? ResponseTypesSupported { get; set; }

        [JsonProperty("code_challenge_methods_supported")]
        public IList<string>? CodeChallengeMethodsSupported { get; set; }

        [JsonProperty("require_pushed_authorization_requests")]
        public bool RequirePushedAuthorizationRequests { get; set; }

        [JsonProperty("request_parameter_supported")]
        public bool RequestParameterSupported { get; set; }

        [JsonProperty("request_uri_parameter_supported")]
        public bool RequestUriParameterSupported { get; set; }

        [JsonProperty("request_object_signing_alg_values_supported")]
        public IList<string>? RequestObjectSigningAlgValuesSupported { get; set; }

        [JsonProperty("tls_client_certificate_bound_access_tokens")]
        public bool TlsClientCertificateBoundAccessTokens { get; set; }

        [JsonProperty("claims_parameter_supported")]
        public bool ClaimsParameterSupported { get; set; }

        [JsonProperty("acr_values_supported")]
        public IList<string>? AcrValuesSupported { get; set; }

        [JsonProperty("token_endpoint_auth_signing_alg_values_supported")]
        public IList<string>? TokenEndpointAuthSigningAlgValuesSupported { get; set; }

        [JsonProperty("token_endpoint_auth_methods_supported")]
        public IList<string>? TokenEndpointAuthMethodsSupported { get; set; }

        [JsonProperty("id_token_signing_alg_values_supported")]
        public IList<string>? IdTokenSigningAlgValuesSupported { get; set; }

        [JsonProperty("id_token_encryption_alg_values_supported")]
        public IList<string>? IdTokenEncryptionAlgValuesSupported { get; set; }

        [JsonProperty("id_token_encryption_enc_values_supported")]
        public IList<string>? IdTokenEncryptionEncValuesSupported { get; set; }

        [JsonProperty("authorization_signing_alg_values_supported")]
        public IList<string>? AuthorizationSigningAlgValuesSupported { get; set; }

        [JsonProperty("authorization_encryption_alg_values_supported")]
        public IList<string>? AuthorizationEncryptionAlgValuesSupported { get; set; }

        [JsonProperty("authorization_encryption_enc_values_supported")]
        public IList<string>? AuthorizationEncryptionEncValuesSupported { get; set; }

        [JsonProperty("mtls_endpoint_aliases")]
        public IDictionary<string, string>? MtlsEndpointAliases { get; set; }

    }
}
