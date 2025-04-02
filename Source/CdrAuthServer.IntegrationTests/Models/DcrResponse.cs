namespace CdrAuthServer.IntegrationTests.Models
{
    public class DcrResponse
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
}
