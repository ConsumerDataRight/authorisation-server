using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CdrAuthServer.Models
{
    public class Client
    {
        /// <summary>
        /// Gets a Data Holder issued client identifier string.
        /// </summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets a time at which the client identifier was issued expressed as seconds since 1970-01-01T00:00:00Z as measured in UTC.
        /// </summary>
        [JsonProperty("client_id_issued_at")]
        public long ClientIdIssuedAt { get; set; }

        /// <summary>
        /// Gets a human-readable string name of the software product to be presented to the end-user during authorization.
        /// </summary>
        [JsonProperty("client_name")]
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets a human-readable string name of the software product description to be presented to the end user during authorization.
        /// </summary>
        [JsonProperty("client_description")]
        public string ClientDescription { get; set; } = string.Empty;

        /// <summary>
        /// URL string of a web page providing information about the client.
        /// </summary>
        [JsonProperty("client_uri")]
        public string ClientUri { get; set; } = string.Empty;

        /// <summary>
        /// Legal Entity Identifier.
        /// </summary>
        [JsonProperty("legal_entity_id")]
        public string LegalEntityId { get; set; } = string.Empty;

        /// <summary>
        /// Legal Entity Name.
        /// </summary>
        [JsonProperty("legal_entity_name")]
        public string LegalEntityName { get; set; } = string.Empty;

        /// <summary>
        /// Gets a unique identifier string assigned by the CDR Register that identifies the Accredited Data Recipient Brand.
        /// </summary>
        [JsonProperty("org_id")]
        public string OrgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets a human-readable string name of the Accredited Data Recipient to be presented to the end user during authorization.
        /// </summary>
        [JsonProperty("org_name")]
        public string OrgName { get; set; } = string.Empty;

        /// <summary>
        /// Gets an array of redirection URI strings for use in redirect-based flows.
        /// </summary>
        [JsonProperty("redirect_uris")]
        public IEnumerable<string> RedirectUris { get; set; }

        /// <summary>
        /// Gets a URL string that references a logo for the client. If present, the server SHOULD display this image to the end-user during approval.
        /// </summary>
        [JsonProperty("logo_uri")]
        public string LogoUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets a URL string that points to a human-readable terms of service document for the Software Product.
        /// </summary>
        [JsonProperty("tos_uri")]
        public string TosUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets a URL string that points to a human-readable policy document for the Software Product.
        /// </summary>
        [JsonProperty("policy_uri")]
        public string PolicyUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets a URL string referencing the client JSON Web Key (JWK) Set [RFC7517] document, which contains the client public keys.
        /// </summary>
        [JsonProperty("jwks_uri")]
        public string JwksUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets a URI string that references the location of the Software Product consent revocation endpoint.
        /// </summary>
        [JsonProperty("revocation_uri")]
        public string RevocationUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a URI string that references the location of the Software Product recipient base uri endpoint.
        /// </summary>
        [JsonProperty("recipient_base_uri")]
        public string RecipientBaseUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets the requested authentication method for the token endpoint.
        /// </summary>
        [JsonProperty("token_endpoint_auth_method")]
        public string TokenEndpointAuthMethod { get; set; } = string.Empty;

        /// <summary>
        /// Gets the algorithm used for signing the JWT.
        /// </summary>
        [JsonProperty("token_endpoint_auth_signing_alg")]
        public string TokenEndpointAuthSigningAlg { get; set; } = string.Empty;

        /// <summary>
        /// Gets an array of OAuth 2.0 grant type strings that the client can use at the token endpoint.
        /// </summary>
        [JsonProperty("grant_types")]
        public IEnumerable<string> GrantTypes { get; set; }

        /// <summary>
        /// Gets an array of the OAuth 2.0 response type strings that the client can use at the authorization endpoint.
        /// </summary>
        [JsonProperty("response_types")]
        public IEnumerable<string> ResponseTypes { get; set; }

        /// <summary>
        /// Gets the kind of the application. The only supported application type will be 'web'.
        /// </summary>
        [JsonProperty("application_type")]
        public string ApplicationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets an algorithm with which an id_token is to be signed.
        /// </summary>
        [JsonProperty("id_token_signed_response_alg")]
        public string IdTokenSignedResponseAlg { get; set; } = string.Empty;

        /// <summary>
        /// Gets a JWE &#x60;alg&#x60; algorithm with which an id_token is to be encrypted.
        /// </summary>
        [JsonProperty("id_token_encrypted_response_alg")]
        public string IdTokenEncryptedResponseAlg { get; set; } = string.Empty;

        /// <summary>
        /// Gets a JWE &#x60;enc&#x60; algorithm with which an id_token is to be encrypted.
        /// </summary>
        [JsonProperty("id_token_encrypted_response_enc")]
        public string IdTokenEncryptedResponseEnc { get; set; } = string.Empty;

        /// <summary>
        /// Gets an algorithm which the ADR expects to sign the request object if a request object will be part of the authorization request sent to the Data Holder.
        /// </summary>
        [JsonProperty("request_object_signing_alg")]
        public string RequestObjectSigningAlg { get; set; } = string.Empty;

        /// <summary>
        /// Gets the Software Statement Assertion, as defined in [Dynamic Client Registration](https://cdr-register.github.io/register/#dynamic-client-registration).
        /// </summary>
        [JsonProperty("software_statement")]
        public string SoftwareStatementJwt { get; set; } = string.Empty;

        /// <summary>
        /// Gets a string representing a unique identifier assigned by the ACCC Register and used by registration endpoints to identify the software product to be dynamically registered. &lt;/br&gt;&lt;/br&gt;The \&quot;software_id\&quot; will remain the same for the lifetime of the product, across multiple updates and versions.
        /// </summary>
        [JsonProperty("software_id")]
        public string SoftwareId { get; set; } = string.Empty;

        /// <summary>
        /// Gets a string representing the software roles as outlined in the SSA.
        /// </summary>
        [JsonProperty("software_roles")]
        public string SoftwareRoles { get; set; } = string.Empty;

        /// <summary>
        /// Gets a string containing a space-separated list of scope values that the client can use when requesting access tokens.
        /// </summary>
        [JsonProperty("scope")]
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Get the sector identifier uri used in PPID calculations.
        /// </summary>
        [JsonProperty("sector_identifier_uri")]
        public string? SectorIdentifierUri { get; set; } = null;

        /// <summary>
        /// Gets an algorithm with which an id_token is to be signed.
        /// </summary>
        [JsonProperty("authorization_signed_response_alg")]
        public string? AuthorizationSignedResponseAlg { get; set; } = null;

        /// <summary>
        /// Gets a JWE &#x60;alg&#x60; algorithm with which an id_token is to be encrypted.
        /// </summary>
        [JsonProperty("authorization_encrypted_response_alg")]
        public string? AuthorizationEncryptedResponseAlg { get; set; } = null;

        /// <summary>
        /// Gets a JWE &#x60;enc&#x60; algorithm with which an id_token is to be encrypted.
        /// </summary>
        [JsonProperty("authorization_encrypted_response_enc")]
        public string? AuthorizationEncryptedResponseEnc { get; set; } = null;
    }
}