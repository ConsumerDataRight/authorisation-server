using System.Collections.Immutable;

namespace CdrAuthServer.Domain
{
    public static class Constants
    {
        public static class ClaimNames
        {
            public const string Confirmation = "cnf";
            public const string Scope = "scope";
            public const string JwksUri = "jwks_uri";
            public const string CdrArrangementId = "cdr_arrangement_id";
            public const string CdrArrangementVersion = "cdr_arrangement_version";
            public const string Expiry = "exp";
            public const string Active = "active";
            public const string ClientId = "client_id";
            public const string ClientAssertion = "client_assertion";
            public const string ClientAssertionType = "client_assertion_type";
            public const string GrantType = "grant_type";
            public const string AccountId = "account_id";
            public const string Acr = "acr";
            public const string JwtId = "jti";
            public const string SoftwareId = "software_id";
            public const string IdToken = "id_token";
            public const string AccessToken = "access_token";
            public const string RefreshToken = "refresh_token";
            public const string Request = "request";
            public const string RequestUri = "request_uri";
            public const string RedirectUri = "redirect_uri";
            public const string ResponseType = "response_type";
            public const string ResponseMode = "response_mode";
            public const string State = "state";
            public const string Nonce = "nonce";
            public const string Code = "code";
            public const string CodeChallenge = "code_challenge";
            public const string CodeChallengeMethod = "code_challenge_method";
            public const string CodeVerifier = "code_verifier";
            public const string Claims = "claims";
            public const string SharingDuration = "sharing_duration";
            public const string IssuedAt = "iat";
            public const string Expiration = "exp";
            public const string Audience = "aud";
            public const string NotBefore = "nbf";
            public const string Issuer = "iss";
            public const string Subject = "sub";
            public const string TokenType = "token_type";
            public const string ExpiresIn = "expires_in";
            public const string Name = "name";
            public const string FamilyName = "family_name";
            public const string GivenName = "given_name";
            public const string SectorIdentifierUri = "sector_identifier_uri";
            public const string AccessTokenHash = "at_hash";
            public const string AuthorizationCodeHash = "c_hash";
            public const string AuthorizationCode = "code";
            public const string StateHash = "s_hash";
            public const string Error = "error";
            public const string ErrorDescription = "error_description";
            public const string AuthTime = "auth_time";
            public const string UpdatedAt = "updated_at";
        }

        public static class TokenTypes
        {
            public const string AccessToken = "at+jwt";
            public const string Jwt = "JWT";
            public const string IdToken = "JWT";
        }

        public static class Algorithms
        {
            public const string None = "none";

            public static class Signing
            {
                public const string ES256 = "ES256";
                public const string PS256 = "PS256";
            }

            public static class Jwe
            {
                public static class Alg
                {
                    public const string RSAOAEP = "RSA-OAEP";
                    public const string RSAOAEP256 = "RSA-OAEP-256";
                }

                public static class Enc
                {
                    public const string A128GCM = "A128GCM";
                    public const string A192GCM = "A192GCM";
                    public const string A256GCM = "A256GCM";
                    public const string A128CBCHS256 = "A128CBC-HS256";
                    public const string A192CBCHS384 = "A192CBC-HS384";
                    public const string A256CBCHS512 = "A256CBC-HS512";
                }
            }
        }

        public static class ClientMetadata
        {
            public const string RedirectUris = "redirect_uris";
            public const string TokenEndpointAuthMethod = "token_endpoint_auth_method";
            public const string TokenEndpointAuthSigningAlg = "token_endpoint_auth_signing_alg";
            public const string ClientMetaDataGrantTypes = "grant_types";
            public const string ClientMetaDataResponseTypes = "response_types";
            public const string ApplicationType = "application_type";
            public const string IdTokenSignedResponseAlg = "id_token_signed_response_alg";
            public const string IdTokenEncryptedResponseAlg = "id_token_encrypted_response_alg";
            public const string IdTokenEncryptedResponseEnc = "id_token_encrypted_response_enc";
            public const string RequestObjectSigningAlg = "request_object_signing_alg";
            public const string SoftwareStatement = "software_statement";
            public const string AuthorizationSignedResponseAlg = "authorization_signed_response_alg";
            public const string AuthorizationEncryptedResponseAlg = "authorization_encrypted_response_alg";
            public const string AuthorizationEncryptedResponseEnc = "authorization_encrypted_response_enc";
        }

        public static class CodeChallengeMethods
        {
            public const string S256 = "S256";
        }

        public static class ErrorCodes
        {
            /// <summary>
            /// The error codes in this class area defined by the CDR program (not CDS)
            /// </summary>
            public static class Generic
            {
                public const string UnsupportedGrantType = "unsupported_grant_type";
                public const string InvalidClient = "invalid_client";
                public const string InvalidRequest = "invalid_request";
                public const string InvalidRequestUri = "invalid_request_uri";
                public const string InvalidGrant = "invalid_grant";
                public const string AccessDenied = "access_denied";
                public const string InvalidRequestObject = "invalid_request_object";
                public const string UnauthorizedClient = "unauthorized_client";
                public const string UnsupportedResponseType = "unsupported_response_type";
                public const string InvalidScope = "invalid_scope";
                public const string InvalidRedirectUri = "invalid_redirect_uri";
                public const string InvalidClientMetadata = "invalid_client_metadata";
                public const string InvalidSoftwareStatement = "invalid_software_statement";
                public const string UnapprovedSoftwareStatement = "unapproved_software_statement";
            }
            /// <summary>
            /// The error codes in this class must match the definition in CDS
            /// </summary>
            public static class Cds
            {
                public const string MissingRequiredHeader = "urn:au-cds:error:cds-all:Header/Missing";
                public const string MissingRequiredField = "urn:au-cds:error:cds-all:Field/Missing";
                public const string InvalidField = "urn:au-cds:error:cds-all:Field/Invalid";
                public const string InvalidDateTime = "urn:au-cds:error:cds-all:Field/InvalidDateTime";
                public const string InvalidPageSize = "urn:au-cds:error:cds-all:Field/InvalidPageSize";
                public const string InvalidPage = "urn:au-cds:error:cds-all:Field/InvalidPage";
                public const string InvalidBrand = "urn:au-cds:error:cds-register:Field/InvalidBrand";
                public const string InvalidIndustry = "urn:au-cds:error:cds-register:Field/InvalidIndustry";
                public const string InvalidSoftwareProduct = "urn:au-cds:error:cds-register:Field/InvalidSoftwareProduct";
                public const string InvalidResource = "urn:au-cds:error:cds-all:Resource/Invalid";
                public const string InvalidHeader = "urn:au-cds:error:cds-all:Header/Invalid";
                public const string InvalidVersion = "urn:au-cds:error:cds-all:Header/InvalidVersion";
                public const string InvalidConsentArrangement = "urn:au-cds:error:cds-all:Authorisation/InvalidArrangement";
                public const string UnexpectedError = "urn:au-cds:error:cds-all:GeneralError/Unexpected";
                public const string ExpectedError = "urn:au-cds:error:cds-all:GeneralError/Expected";
                public const string ServiceUnavailable = "urn:au-cds:error:cds-all:Service/Unavailable";
                public const string AdrStatusNotActive = "urn:au-cds:error:cds-all:Authorisation/AdrStatusNotActive";
                public const string RevokedConsent = "urn:au-cds:error:cds-all:Authorisation/RevokedConsent";
                public const string InvalidConsent = "urn:au-cds:error:cds-all:Authorisation/InvalidConsent";
                public const string ResourceNotImplemented = "urn:au-cds:error:cds-all:Resource/NotImplemented";
                public const string ResourceNotFound = "urn:au-cds:error:cds-all:Resource/NotFound";
                public const string UnsupportedVersion = "urn:au-cds:error:cds-all:Header/UnsupportedVersion";
                public const string UnavailableResource = "urn:au-cds:error:cds-all:Resource/Unavailable";
            }
        }

        public static class ErrorTitles
        {
            public const string MissingVersion = "Missing Version";
            public const string UnsupportedVersion = "Unsupported Version";
            public const string InvalidVersion = "Invalid Version";
            public const string ExpectedError = "Expected Error Encountered";
            public const string UnexpectedError = "Unexpected Error Encountered";
            public const string ServiceUnavailable = "Service Unavailable";
            public const string MissingRequiredField = "Missing Required Field";
            public const string MissingRequiredHeader = "Missing Required Header";
            public const string InvalidField = "Invalid Field";
            public const string InvalidHeader = "Invalid Header";
            public const string InvalidDate = "Invalid Date";
            public const string InvalidDateTime = "Invalid DateTime";
            public const string InvalidPageSize = "Invalid Page Size";
            public const string ADRStatusNotActive = "ADR Status Is Not Active";
            public const string RevokedConsent = "Consent Is Revoked";
            public const string InvalidConsent = "Consent Is Invalid";
            public const string ResourceNotImplemented = "Resource Not Implemented";
            public const string ResourceNotFound = "Resource Not Found";
            public const string InvalidConsentArrangement = "Invalid Consent Arrangement";
            public const string InvalidPage = "Invalid Page";
            public const string InvalidResource = "Invalid Resource";
            public const string UnavailableResource = "Unavailable Resource";
            public const string InvalidBrand = "Invalid Brand";
            public const string InvalidIndustry = "Invalid Industry";
            public const string InvalidSoftwareProduct = "Invalid Software Product";
        }

        public static class ValidationErrorMessages
        {
            public const string MissingClaim = "The '{0}' claim is missing.";
            public const string MustEqual = "The '{0}' claim value must be set to '{1}'.";
            public const string MustBeOne = "The '{0}' claim value must be one of '{1}'.";
            public const string MustContain = "The '{0}' claim value must contain the '{1}' value.";
            public const string InvalidRedirectUri = "One or more redirect uri is invalid";
        }

        public static class GrantTypes
        {
            public const string CdrArrangement = "cdr_arrangement";
            public const string RefreshToken = "refresh_token";
            public const string AuthCode = "authorization_code";
            public const string ClientCredentials = "client_credentials";
            public const string Hybrid = "hybrid";
            public const string RequestUri = "request_uri";
        }

        public static class ResponseModes
        {
            public const string FormPost = "form_post";
            public const string Fragment = "fragment";
            public const string FormPostJwt = "form_post.jwt";
            public const string FragmentJwt = "fragment.jwt";
            public const string QueryJwt = "query.jwt";
            public const string Jwt = "jwt";
        }

        public static class ResponseTypes
        {
            public const string AuthCode = "code";
            public const string Hybrid = "code id_token";
        }

        // The order of the response modes represents the precedence for each response type.
        public static readonly ImmutableDictionary<string, string[]> SupportedResponseModesForResponseType = new Dictionary<string, string[]>
        {
            { ResponseTypes.Hybrid, new string[] { ResponseModes.Fragment, ResponseModes.FormPost } },
            { ResponseTypes.AuthCode, new string[] { ResponseModes.QueryJwt, ResponseModes.FragmentJwt, ResponseModes.FormPostJwt, ResponseModes.Jwt } },
        }.ToImmutableDictionary();

        public static class ValidationRestrictions
        {
            public static class InputLengthRestrictions
            {
                public const int ScopeMaxLength = 1000;

                // Using the same values as code verifier.  Not necessarily correct
                // as the value will be a SHA256 string (64 chars) but is also base64 url encoded.
                public const int CodeChallengeMinLength = 43;
                public const int CodeChallengeMaxLength = 128;

                public const int CodeVerifierMinLength = 43;
                public const int CodeVerifierMaxLength = 128;
            }
        }

        public static class EntityStatus
        {
            public const string Active = "ACTIVE";
            public const string InActive = "INACTIVE";
            public const string Removed = "REMOVED";
        }
    }
}
