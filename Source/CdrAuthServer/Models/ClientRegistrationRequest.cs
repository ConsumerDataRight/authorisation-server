using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class ClientRegistrationRequest : JwtSecurityToken
    {
        public ClientRegistrationRequest()
        {
        }

        public ClientRegistrationRequest(string jwt)
            : base(jwt)
        {
            ClientRegistrationRequestJwt = jwt;
        }

        /// <summary>
        /// Gets a Software Statement Assertion, as defined in [Dynamic Client Registration](https://cdr-register.github.io/register/#dynamic-client-registration).
        /// </summary>
        [Display(Name = "software_statement")]
        public SoftwareStatement? SoftwareStatement { get; set; }

        public string ClientRegistrationRequestJwt { get; } = string.Empty;

        /// <summary>
        /// Gets the Key Identifier of this JWT.
        /// </summary>
        public string Kid => Header.Kid;

        /// <summary>
        /// Gets the time at which the request was issued by the TPP  expressed as seconds since 1970-01-01T00:00:00Z as measured in UTC.
        /// </summary>
        [Display(Name = "iat")]
        public int? Iat
        {
            get
            {
                if (int.TryParse(Claims.FirstOrDefault(x => x.Type == ClaimNames.IssuedAt)?.Value, out int value))
                {
                    return value;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the time at which the request expires expressed as seconds since 1970-01-01T00:00:00Z as measured in UTC.
        /// </summary>
        [Display(Name = "exp")]
        public int? Exp
        {
            get
            {
                if (int.TryParse(Claims.FirstOrDefault(x => x.Type == ClaimNames.Expiration)?.Value, out int value))
                {
                    return value;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a unique identifier for the Data Holder issued by the CDR Register.
        /// </summary>
        [Display(Name = "iss")]
        public string? Iss => Claims.FirstOrDefault(x => x.Type == ClaimNames.Issuer)?.Value;

        /// <summary>
        /// Gets a unique identifier for the JWT, used to prevent replay of the token.
        /// </summary>
        [Display(Name = "jti")]
        public string? Jti => Claims.FirstOrDefault(x => x.Type == ClaimNames.JwtId)?.Value;

        /// <summary>
        /// Gets the audience for the request. This should be the Data Holder authorisation server URI.
        /// </summary>
        [Display(Name = "aud")]
        public string? Aud => Claims.FirstOrDefault(x => x.Type == ClaimNames.Audience)?.Value;

        /// <summary>
        /// Gets an array of redirection URI strings for use in redirect-based flows.
        /// </summary>
        [Display(Name = "redirect_uris")]
        public IEnumerable<string> RedirectUris => Claims.Where(x => x.Type == ClientMetadata.RedirectUris).Select(x => x.Value);

        /// <summary>
        /// Gets the requested authentication method for the token endpoint.
        /// </summary>
        [Display(Name = "token_endpoint_auth_method")]
        public string? TokenEndpointAuthMethod
        {
            get => Claims.FirstOrDefault(x => x.Type == ClientMetadata.TokenEndpointAuthMethod)?.Value;
        }

        /// <summary>
        /// Gets the algorithm used for signing the JWT.
        /// </summary>
        [Display(Name = "token_endpoint_auth_signing_alg")]
        public string? TokenEndpointAuthSigningAlg => Claims.FirstOrDefault(x => x.Type == ClientMetadata.TokenEndpointAuthSigningAlg)?.Value;

        /// <summary>
        /// Gets an array of OAuth 2.0 grant type strings that the client can use at the token endpoint.
        /// </summary>
        [Display(Name = "grant_types")]
        public IEnumerable<string> GrantTypes => Claims.Where(x => x.Type == ClientMetadata.ClientMetaDataGrantTypes).Select(x => x.Value);

        /// <summary>
        /// Gets an array of the OAuth 2.0 response type strings that the client can use at the authorization endpoint.
        /// </summary>
        [Display(Name = "response_types")]
        public IEnumerable<string> ResponseTypes => Claims.Where(x => x.Type == ClientMetadata.ClientMetaDataResponseTypes).Select(x => x.Value);

        /// <summary>
        /// Gets the kind of the application. The only supported application type will be 'web'.
        /// </summary>
        [Display(Name = "application_type")]
        public string? ApplicationType => Claims.FirstOrDefault(x => x.Type == ClientMetadata.ApplicationType)?.Value;

        /// <summary>
        /// Gets a algorithm with which an id_token is to be signed.
        /// </summary>
        [Display(Name = "id_token_signed_response_alg")]
        public string? IdTokenSignedResponseAlg => Claims.FirstOrDefault(x => x.Type == ClientMetadata.IdTokenSignedResponseAlg)?.Value;

        /// <summary>
        /// Gets an algorithm which the ADR expects to sign the request object if a request object will be part of the authorization request sent to the Data Holder.
        /// </summary>
        [Display(Name = "request_object_signing_alg")]
        public string? RequestObjectSigningAlg => Claims.FirstOrDefault(x => x.Type == ClientMetadata.RequestObjectSigningAlg)?.Value;

        /// <summary>
        /// Gets the Software Statement Assertion.
        /// </summary>
        [Display(Name = "software_statement")]
        public string? SoftwareStatementJwt => Claims.FirstOrDefault(x => x.Type == ClientMetadata.SoftwareStatement)?.Value;

        /// <summary>
        /// Gets a algorithm with which an auth request is to be signed (JARM).
        /// </summary>
        [Display(Name = "authorization_signed_response_alg")]
        public string? AuthorizationSignedResponseAlg => Claims.FirstOrDefault(x => x.Type == ClientMetadata.AuthorizationSignedResponseAlg)?.Value;

        /// <summary>
        /// Gets a JWE &#x60;alg&#x60; algorithm with which an auth request is to be encrypted (JARM).
        /// </summary>
        [Display(Name = "authorization_encrypted_response_alg")]
        public string? AuthorizationEncryptedResponseAlg => Claims.FirstOrDefault(x => x.Type == ClientMetadata.AuthorizationEncryptedResponseAlg)?.Value;

        /// <summary>
        /// Gets a JWE &#x60;enc&#x60; algorithm with which an auth request is to be encrypted (JARM).
        /// </summary>
        [Display(Name = "authorization_encrypted_response_enc")]
        public string? AuthorizationEncryptedResponseEnc => Claims.FirstOrDefault(x => x.Type == ClientMetadata.AuthorizationEncryptedResponseEnc)?.Value;
    }
}
