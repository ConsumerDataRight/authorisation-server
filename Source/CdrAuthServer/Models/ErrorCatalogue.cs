using CdrAuthServer.Models;
using CdrAuthServer.Validation;
using Jose;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer
{
    public class ErrorCatalogue
    {
        static ErrorCatalogue _instance;
        private static readonly object locker = new object();
        public static IDictionary<string, ErrorDefinition> _errorCatalogue = new Dictionary<string, ErrorDefinition>();

        public static class Categories
        {
            public const string General = "General";
            public const string ClientAssertion = "ClientAssertion";
            public const string Mtls = "MTLS";
            public const string PushedAuthorizationRequest = "PAR";
            public const string Authorization = "Authorization";
            public const string Token = "Token";
            public const string DCR = "DCR";
            public const string Arrangement = "Arrangement";
            public const string JWT = "JWT";
        }

        // PAR
        public const string PAR_REQUEST_URI_FORM_PARAMETER_NOT_SUPPORTED = "ERR-PAR-001";
        public const string PAR_REQUEST_IS_NOT_WELL_FORMED_JWT = "ERR-PAR-002";
        public const string UNSUPPORTED_CHALLENGE_METHOD = "ERR-PAR-003";
        public const string CODE_CHALLENGE_INVALID_LENGTH = "ERR-PAR-004";
        public const string CODE_CHALLENGE_MISSING = "ERR-PAR-005";
        public const string REQUEST_OBJECT_JWT_REQUEST_URI_NOT_SUPPORTED = "ERR-PAR-006";
        public const string REQUEST_OBJECT_JWT_REDIRECT_URI_MISSING = "ERR-PAR-007";
        public const string REQUEST_OBJECT_JWT_CLIENT_ID_MISMATCH = "ERR-PAR-008";
        public const string MISSING_RESPONSE_MODE = "ERR-PAR-009";
        public const string RESPONSE_TYPE_NOT_REGISTERED = "ERR-PAR-010";

        // MTLS
        public const string MTLS_MULTIPLE_THUMBPRINTS = "ERR-MTLS-001";
        public const string MTLS_NO_CERTIFICATE = "ERR-MTLS-002";
        public const string MTLS_CERT_OCSP_FAILED = "ERR-MTLS-003";
        public const string MTLS_CERT_OCSP_ERROR = "ERR-MTLS-004";

        //Auth
        public const string AUTHORIZATION_HOLDER_OF_KEY_CHECK_FAILED = "ERR-AUTH-001";
        public const string AUTHORIZATION_ACCESS_TOKEN_REVOKED = "ERR-AUTH-002";
        public const string AUTHORIZATION_INSUFFICIENT_SCOPE = "ERR-AUTH-003";
        public const string REQUEST_URI_CLIENT_ID_MISMATCH = "ERR-AUTH-0004";
        public const string REQUEST_URI_ALREADY_USED = "ERR-AUTH-005";
        public const string REQUEST_URI_EXPIRED = "ERR-AUTH-006";
        public const string INVALID_REQUEST_URI = "ERR-AUTH-007";
        public const string REQUEST_URI_MISSING = "ERR-AUTH-008";
        public const string ACCESS_DENIED = "ERR-AUTH-009";

        //Client Assertion
        public const string CLIENT_ASSERTION_TYPE_NOT_PROVIDED = "ERR-CLIENT_ASSERTION-002";
        public const string INVALID_CLIENT_ASSERTION_TYPE = "ERR-CLIENT_ASSERTION-003";
        public const string CLIENT_ASSERTION_CLIENT_ID_MISMATCH = "ERR-CLIENT_ASSERTION-004";
        public const string CLIENT_ASSERTION_INVALID_FORMAT = "ERR-CLIENT_ASSERTION-005";
        public const string CLIENT_ASSERTION_NOT_READABLE = "ERR-CLIENT_ASSERTION-006";
        public const string CLIENT_ASSERTION_SUBJECT_ISS_NOT_SET_TO_CLIENT_ID = "ERR-CLIENT_ASSERTION-007";
        public const string CLIENT_ASSERTION_SUBJECT_ISS_NOT_SAME_VALUE = "ERR-CLIENT_ASSERTION-008";
        public const string CLIENT_ASSERTION_MISSING_ISS_CLAIM = "ERR-CLIENT_ASSERTION-009";
        

        //CDR Arrangement
        public const string INVALID_CONSENT_CDR_ARRANGEMENT = "ERR-ARR-001";

        //JWT
        public const string JWT_INVALID_AUDIENCE = "ERR-JWT-001";
        public const string JWT_EXPIRED = "ERR-JWT-002";
        public const string JWKS_ERROR = "ERR-JWT-003";
        public const string JWT_VALIDATION_ERROR = "ERR-JWT-004";

        //DCR
        public const string DUPLICATE_REGISTRATION = "ERR-DCR-001";
        public const string EMPTY_REGISTRATION_REQUEST = "ERR-DCR-002";
        public const string REGISTRATION_REQUEST_INVALID_REDIRECT_URI = "ERR-DCR-003";
        public const string REGISTRATION_REQUEST_VALIDATION_FAILED = "ERR-DCR-004";
        public const string SSA_VALIDATION_FAILED = "ERR-DCR-005";
        public const string SOFTWARE_STATEMENT_INVALID_OR_EMPTY = "ERR-DCR-006";
        public const string INVALID_SECTOR_IDENTIFIER_URI = "ERR-DCR-007";


        //Token
        public const string REFRESH_TOKEN_EXPIRED = "ERR-TKN-001";
        public const string INVALID_REFRESH_TOKEN = "ERR-TKN-002";
        public const string REFRESH_TOKEN_MISSING = "ERR-TKN-003";
        public const string INVALID_CODE_VERIFIER = "ERR-TKN-004";
        public const string AUTHORIZATION_CODE_EXPIRED = "ERR-TKN-005";
        public const string CODE_VERIFIER_IS_MISSING = "ERR-TKN-006";
        public const string INVALID_AUTHORIZATION_CODE = "ERR-TKN-007";


        //General
        public const string SOFTWARE_PRODUCT_NOT_FOUND = "ERR-GEN-001";
        public const string SOFTWARE_PRODUCT_STATUS_INACTIVE = "ERR-GEN-002";
        public const string SOFTWARE_PRODUCT_REMOVED = "ERR-GEN-003";
        public const string CLIENT_NOT_FOUND = "ERR-GEN-004";
        public const string CLIENT_ID_MISSING = "ERR-GEN-005";
        public const string INVALID_CLIENT_ID = "ERR-GEN-006";
        public const string INVALID_REDIRECT_URI = "ERR-GEN-007";
        public const string RESPONSE_TYPE_MISSING = "ERR-GEN-008";
        public const string RESPONSE_TYPE_NOT_SUPPORTED = "ERR-GEN-009";
        public const string RESPONSE_TYPE_MISMATCH_REQUEST_URI_RESPONSE_TYPE = "ERR-GEN-010";
        public const string SCOPE_MISSING = "ERR-GEN-011";
        public const string OPEN_ID_SCOPE_MISSING = "ERR-GEN-012";
        public const string INVALID_RESPONSE_MODE = "ERR-GEN-013";
        public const string GRANT_TYPE_NOT_PROVIDED = "ERR-GEN-014";
        public const string UNSUPPORTED_GRANT_TYPE = "ERR-GEN-015";
        public const string MISSING_ISSUER_CLAIM = "ERR-GEN-016";
        public const string JTI_REQUIRED = "ERR-GEN-017";
        public const string JTI_NOT_UNIQUE = "ERR-GEN-018";
        public const string CLIENT_ASSERTION_NOT_PROVIDED = "ERR-GEN-019";
        public const string INVALID_JWKS_URI = "ERR-GEN-020";
        public const string UNABLE_TO_LOAD_JWKS_DATA_RECIPIENT = "ERR-GEN-021";
        public const string UNABLE_TO_LOAD_JWKS_FROM_REGISTER = "ERR-GEN-022";
        public const string EXP_MISSING = "ERR-GEN-023";
        public const string NBF_MISSING = "ERR-GEN-024";
        public const string EXPIRY_GREATER_THAN_60_AFTER_NBF = "ERR-GEN-025";
        public const string INVALID_RESPONSE_MODE_FOR_RESPONSE_TYPE = "ERR-GEN-026";
        public const string SCOPE_TOO_LONG = "ERR-GEN-027";
        public const string INVALID_CLAIMS = "ERR-GEN-028";
        public const string INVALID_CDR_ARRANGEMENT_ID = "ERR-GEN-029";
        public const string INVALID_NONCE = "ERR-GEN-030";
        public const string INVALID_TOKEN_REQUEST = "ERR-GEN-031";
        public const string GRANT_TYPE_MISSING = "ERR-GEN-032";
        public const string CLIENT_ID_MISMATCH = "ERR-GEN-033";
        public const string UNABLE_TO_RETRIEVE_CLIENT_META_DATA = "ERR-GEN-034";
        public const string CODE_IS_MISSING = "ERR-GEN-035";
        public const string REDIRECT_URI_IS_MISSING = "ERR-GEN-036";
        public const string REDIRECT_URI_AUTHORIZATION_REQUEST_MISMATCH = "ERR-GEN-037";
        public const string INVALID_CLIENT = "ERR-GEN-038";

        protected ErrorCatalogue()
        {
            // PAR errors.
            AddToCatalogue(PAR_REQUEST_URI_FORM_PARAMETER_NOT_SUPPORTED, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "request_uri form parameter is not supported", StatusCodes.Status400BadRequest);
            AddToCatalogue(PAR_REQUEST_IS_NOT_WELL_FORMED_JWT, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "request is not a well-formed JWT", StatusCodes.Status400BadRequest);
            AddToCatalogue(UNSUPPORTED_CHALLENGE_METHOD, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "Unsupported code_challenge_method", StatusCodes.Status400BadRequest);
            AddToCatalogue(CODE_CHALLENGE_INVALID_LENGTH, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "Invalid code_challenge - invalid length", StatusCodes.Status400BadRequest);
            AddToCatalogue(CODE_CHALLENGE_MISSING, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "code_challenge is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(REQUEST_OBJECT_JWT_REQUEST_URI_NOT_SUPPORTED, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequestObject, "request_uri is not supported in request object", StatusCodes.Status400BadRequest);
            AddToCatalogue(REQUEST_OBJECT_JWT_REDIRECT_URI_MISSING, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequestObject, "redirect_uri missing from request object JWT", StatusCodes.Status400BadRequest);
            AddToCatalogue(REQUEST_OBJECT_JWT_CLIENT_ID_MISMATCH, Categories.PushedAuthorizationRequest, ErrorCodes.UnauthorizedClient, "client_id does not match client_id in request object JWT", StatusCodes.Status400BadRequest);
            AddToCatalogue(MISSING_RESPONSE_MODE, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "response_mode is missing or not set to 'jwt' for response_type of 'code'", StatusCodes.Status400BadRequest);
            AddToCatalogue(RESPONSE_TYPE_NOT_REGISTERED, Categories.PushedAuthorizationRequest, ErrorCodes.InvalidRequest, "response_type is not registered for the client. Check client registration metadata.", StatusCodes.Status400BadRequest);

            // MTLS errors.
            AddToCatalogue(MTLS_MULTIPLE_THUMBPRINTS, Categories.Mtls, "client_certificate_error", "Multiple certificate thumbprints found on request", StatusCodes.Status403Forbidden);
            AddToCatalogue(MTLS_NO_CERTIFICATE, Categories.Mtls, "client_certificate_required", "No client certificate was found on request", StatusCodes.Status403Forbidden);
            AddToCatalogue(MTLS_CERT_OCSP_ERROR, Categories.Mtls, "certificate_ocsp_invalid", "Certificate status check: {0}", StatusCodes.Status400BadRequest);
            AddToCatalogue(MTLS_CERT_OCSP_FAILED, Categories.Mtls, "certificate_ocsp_failed", "Certificate status check failed for {0} with result: {1}", StatusCodes.Status400BadRequest);

            // Authorization errors.
            AddToCatalogue(AUTHORIZATION_HOLDER_OF_KEY_CHECK_FAILED, Categories.Authorization, "invalid_token", "Holder of Key check failed", StatusCodes.Status401Unauthorized);
            AddToCatalogue(AUTHORIZATION_ACCESS_TOKEN_REVOKED, Categories.Authorization, "invalid_token", "Access Token check failed - it has been revoked", StatusCodes.Status401Unauthorized);
            AddToCatalogue(AUTHORIZATION_INSUFFICIENT_SCOPE, Categories.Authorization, "insufficient_scope", "", StatusCodes.Status403Forbidden);
            AddToCatalogue(REQUEST_URI_ALREADY_USED, Categories.Authorization, ErrorCodes.InvalidRequestUri, "request_uri has already been used", StatusCodes.Status400BadRequest);
            AddToCatalogue(REQUEST_URI_CLIENT_ID_MISMATCH, Categories.Authorization, ErrorCodes.InvalidRequest, "client_id does not match request_uri client_id", StatusCodes.Status400BadRequest);
            AddToCatalogue(REQUEST_URI_EXPIRED, Categories.Authorization, ErrorCodes.InvalidRequestUri, "request_uri has expired", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_REQUEST_URI, Categories.Authorization, ErrorCodes.InvalidRequest, "Invalid request_uri", StatusCodes.Status400BadRequest);
            AddToCatalogue(REQUEST_URI_MISSING, Categories.Authorization, ErrorCodes.InvalidRequest, "request_uri is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(ACCESS_DENIED, Categories.Authorization, ErrorCodes.AccessDenied, "User cancelled the authorisation flow", StatusCodes.Status400BadRequest);

            // DCR.
            AddToCatalogue(DUPLICATE_REGISTRATION, Categories.DCR, ErrorCodes.InvalidClientMetadata, "Duplicate registrations for a given software_id are not valid.", StatusCodes.Status400BadRequest);
            AddToCatalogue(EMPTY_REGISTRATION_REQUEST, Categories.DCR, ErrorCodes.InvalidClientMetadata, "Registration request is empty", StatusCodes.Status400BadRequest);
            AddToCatalogue(REGISTRATION_REQUEST_INVALID_REDIRECT_URI, Categories.DCR, ErrorCodes.InvalidRedirectUri, @"The redirect_uri '{0}' is not valid as it is not included in the software_statement", 401);
            AddToCatalogue(REGISTRATION_REQUEST_VALIDATION_FAILED, Categories.DCR, ErrorCodes.InvalidClientMetadata, "Client Registration Request validation failed.", StatusCodes.Status401Unauthorized);
            AddToCatalogue(SSA_VALIDATION_FAILED, Categories.DCR, ErrorCodes.InvalidSoftwareStatement, "SSA validation failed.", StatusCodes.Status401Unauthorized);
            AddToCatalogue(SOFTWARE_STATEMENT_INVALID_OR_EMPTY, Categories.DCR, ErrorCodes.InvalidSoftwareStatement, "The software_statement is empty or invalid", StatusCodes.Status401Unauthorized);

            //Client Assertion errors


            //token errors
            AddToCatalogue(CLIENT_NOT_FOUND, Categories.Token, ErrorCodes.InvalidClient, "Client not found", StatusCodes.Status400BadRequest);
            AddToCatalogue(REFRESH_TOKEN_EXPIRED, Categories.Token, ErrorCodes.InvalidGrant, "refresh_token has expired", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_REFRESH_TOKEN, Categories.Token, ErrorCodes.InvalidGrant, "refresh_token is invalid", StatusCodes.Status400BadRequest);
            AddToCatalogue(REFRESH_TOKEN_MISSING, Categories.Token, ErrorCodes.InvalidGrant, "refresh_token is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_CODE_VERIFIER, Categories.Token, ErrorCodes.InvalidGrant, "Invalid code_verifier", StatusCodes.Status400BadRequest);
            AddToCatalogue(AUTHORIZATION_CODE_EXPIRED, Categories.Token, ErrorCodes.InvalidGrant, "authorization code has expired", StatusCodes.Status400BadRequest);
            AddToCatalogue(CODE_VERIFIER_IS_MISSING, Categories.Token, ErrorCodes.InvalidGrant, "code_verifier is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_AUTHORIZATION_CODE, Categories.Token, ErrorCodes.InvalidGrant, "authorization code is invalid", StatusCodes.Status400BadRequest);

            //arrangement errors
            AddToCatalogue(INVALID_CONSENT_CDR_ARRANGEMENT, Categories.Arrangement, ErrorCodes.InvalidConsentArrangement, "Invalid Consent Arrangement", StatusCodes.Status422UnprocessableEntity);

            //jwt errors
            AddToCatalogue(JWT_INVALID_AUDIENCE, Categories.JWT, ErrorCodes.InvalidClient, @"{0} - Invalid audience", StatusCodes.Status400BadRequest);
            AddToCatalogue(JWT_EXPIRED, Categories.JWT, ErrorCodes.InvalidClient, @"{0} has expired", StatusCodes.Status400BadRequest);
            AddToCatalogue(JWKS_ERROR, Categories.JWT, ErrorCodes.InvalidClient, @"{0} - jwks error", StatusCodes.Status400BadRequest);
            AddToCatalogue(JWT_VALIDATION_ERROR, Categories.JWT, ErrorCodes.InvalidClient, @"{0} - token validation error", StatusCodes.Status400BadRequest);

            //general errors
            AddToCatalogue(SOFTWARE_PRODUCT_NOT_FOUND, Categories.General, ErrorCodes.InvalidClient, "Software product not found", StatusCodes.Status403Forbidden, true);
            AddToCatalogue(SOFTWARE_PRODUCT_STATUS_INACTIVE, Categories.General, ErrorCodes.AdrStatusNotActive, "Software product status is {0}", StatusCodes.Status403Forbidden, true, "ADR Status Is Not Active");
            AddToCatalogue(SOFTWARE_PRODUCT_REMOVED, Categories.General, ErrorCodes.AdrStatusNotActive, "Software product status is removed - consents cannot be revoked", StatusCodes.Status403Forbidden, true, "ADR Status Is Not Active");
            AddToCatalogue(CLIENT_ID_MISSING, Categories.General, ErrorCodes.InvalidRequest, "client_id is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_CLIENT_ID, Categories.General, ErrorCodes.InvalidRequest, "Invalid client_id", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_REDIRECT_URI, Categories.General, ErrorCodes.InvalidRequest, "Invalid redirect_uri for client", StatusCodes.Status400BadRequest);
            AddToCatalogue(RESPONSE_TYPE_MISSING, Categories.General, ErrorCodes.InvalidRequest, "response_type is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(RESPONSE_TYPE_NOT_SUPPORTED, Categories.General, ErrorCodes.InvalidRequest, "response_type is not supported", StatusCodes.Status400BadRequest);
            AddToCatalogue(RESPONSE_TYPE_MISMATCH_REQUEST_URI_RESPONSE_TYPE, Categories.General, ErrorCodes.InvalidRequest, "response_type does not match request_uri response_type", StatusCodes.Status400BadRequest);
            AddToCatalogue(SCOPE_MISSING, Categories.General, ErrorCodes.InvalidRequest, "scope is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(OPEN_ID_SCOPE_MISSING, Categories.General, ErrorCodes.InvalidRequest, "openid scope is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_RESPONSE_MODE, Categories.General, ErrorCodes.InvalidRequest, "response_mode is not supported", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_NOT_PROVIDED, Categories.General, ErrorCodes.InvalidClient, "client_assertion not provided", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_TYPE_NOT_PROVIDED, Categories.ClientAssertion, ErrorCodes.InvalidClient, "client_assertion_type not provided", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_CLIENT_ASSERTION_TYPE, Categories.ClientAssertion, ErrorCodes.InvalidClient, "client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer", StatusCodes.Status400BadRequest);
            AddToCatalogue(GRANT_TYPE_NOT_PROVIDED, Categories.General, ErrorCodes.UnsupportedGrantType, "grant_type not provided", StatusCodes.Status400BadRequest);
            AddToCatalogue(UNSUPPORTED_GRANT_TYPE, Categories.General, ErrorCodes.UnsupportedGrantType, "unsupported grant_type", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_CLIENT_ID_MISMATCH, Categories.ClientAssertion, ErrorCodes.InvalidClient, "client_id does not match client_assertion", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_INVALID_FORMAT, Categories.ClientAssertion, ErrorCodes.InvalidClient, "Cannot read client_assertion.  Invalid format.", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_NOT_READABLE, Categories.ClientAssertion, ErrorCodes.InvalidClient, "Cannot read client_assertion", StatusCodes.Status400BadRequest);
            AddToCatalogue(MISSING_ISSUER_CLAIM, Categories.General, ErrorCodes.InvalidClient, "Missing iss claim", StatusCodes.Status400BadRequest);
            AddToCatalogue(JTI_REQUIRED, Categories.General, ErrorCodes.InvalidClient, "Invalid client_assertion - 'jti' is required", StatusCodes.Status400BadRequest);
            AddToCatalogue(JTI_NOT_UNIQUE, Categories.General, ErrorCodes.InvalidClient, "Invalid client_assertion - 'jti' must be unique", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_SUBJECT_ISS_NOT_SET_TO_CLIENT_ID, Categories.ClientAssertion, ErrorCodes.InvalidClient, "Invalid client_assertion - 'sub' and 'iss' must be set to the client_id", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_SUBJECT_ISS_NOT_SAME_VALUE, Categories.ClientAssertion, ErrorCodes.InvalidClient, "Invalid client_assertion - 'sub' and 'iss' must have the same value", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ASSERTION_MISSING_ISS_CLAIM, Categories.ClientAssertion, ErrorCodes.InvalidClient, "Invalid client_assertion - Missing 'iss' claim", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_JWKS_URI, Categories.General, ErrorCodes.InvalidClientMetadata, "Invalid jwks_uri in SSA", StatusCodes.Status400BadRequest);
            AddToCatalogue(UNABLE_TO_LOAD_JWKS_DATA_RECIPIENT, Categories.General, ErrorCodes.InvalidClientMetadata, "Could not load JWKS from Data Recipient endpoint: {0}", StatusCodes.Status500InternalServerError);
            AddToCatalogue(INVALID_SECTOR_IDENTIFIER_URI, Categories.General, ErrorCodes.InvalidClientMetadata, "Invalid sector_identifier_uri", StatusCodes.Status400BadRequest);
            AddToCatalogue(UNABLE_TO_LOAD_JWKS_FROM_REGISTER, Categories.General, ErrorCodes.InvalidSoftwareStatement, "Could not load SSA JWKS from Register endpoint: {0}", StatusCodes.Status500InternalServerError);
            AddToCatalogue(EXP_MISSING, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid request - exp is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(NBF_MISSING, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid request - nbf is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(EXPIRY_GREATER_THAN_60_AFTER_NBF, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid request - exp claim cannot be longer than 60 minutes after the nbf claim", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_RESPONSE_MODE_FOR_RESPONSE_TYPE, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid response_mode for response_type", StatusCodes.Status400BadRequest);
            AddToCatalogue(SCOPE_TOO_LONG, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid scope - scope is too long", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_CLAIMS, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid claims in request object", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_CDR_ARRANGEMENT_ID, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid cdr_arrangement_id", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_NONCE, Categories.Authorization, ErrorCodes.InvalidRequestObject, "Invalid nonce", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_TOKEN_REQUEST, Categories.Authorization, ErrorCodes.InvalidRequest, "invalid token request", StatusCodes.Status400BadRequest);
            AddToCatalogue(GRANT_TYPE_MISSING, Categories.Authorization, ErrorCodes.InvalidRequest, "grant_type is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(CLIENT_ID_MISMATCH, Categories.Authorization, ErrorCodes.InvalidRequest, "client_id does not match", StatusCodes.Status400BadRequest);
            AddToCatalogue(UNABLE_TO_RETRIEVE_CLIENT_META_DATA, Categories.Authorization, ErrorCodes.InvalidRequest, "Could not retrieve client metadata", StatusCodes.Status400BadRequest);
            AddToCatalogue(CODE_IS_MISSING, Categories.Authorization, ErrorCodes.InvalidRequest, "code is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(REDIRECT_URI_IS_MISSING, Categories.Authorization, ErrorCodes.InvalidRequest, "redirect_uri is missing", StatusCodes.Status400BadRequest);
            AddToCatalogue(REDIRECT_URI_AUTHORIZATION_REQUEST_MISMATCH, Categories.Authorization, ErrorCodes.InvalidRequest, "redirect_uri does not match authorization request", StatusCodes.Status400BadRequest);
            AddToCatalogue(INVALID_CLIENT, Categories.Authorization, ErrorCodes.InvalidClient, "invalid_client", StatusCodes.Status400BadRequest);
        }

        private void AddToCatalogue(
            string code,
            string category,
            string error,
            string errorDescription,
            int statusCode = 400,
            bool isCdsError = false,
            string errorTitle = null)
        {
            _errorCatalogue.Add(code, new ErrorDefinition(category, code, error, $"{code}: {errorDescription}", statusCode, isCdsError, errorTitle));
        }

        public static ErrorCatalogue Catalogue()
        {
            if (_instance == null)
            {
                lock (locker)
                {
                    if (_instance == null)
                    {
                        _instance = new ErrorCatalogue();
                    }
                }
            }
            return _instance;
        }

        public (Error, int) GetError(string code)
        {
            var errorDefinition = _errorCatalogue[code];
            if (errorDefinition == null)
            {
                return (new Error(ErrorCodes.UnexpectedError), 400);
            }

            return (new Error(errorDefinition.Error, errorDefinition.ErrorDescription), errorDefinition.StatusCode);
        }

        public ErrorDefinition GetErrorDefinition(string code)
        {
            var errorDefinition = _errorCatalogue[code];

            if (errorDefinition == null)
            {
                return new ErrorDefinition(
                    Categories.General,
                    ErrorCodes.UnexpectedError,
                    "urn:au-cds:error:cds-all:GeneralError/Unexpected",
                    "{0}",
                    400,
                    true,
                    "Unexpected Error Encountered");
            }

            return errorDefinition;
        }

        public (Error, int) GetError(string code, string? context)
        {
            var errorDefinition = GetErrorDefinition(code);
            var errorDescription = errorDefinition.ErrorDescription;
            if (context != null)
            {
                errorDescription = string.Format(errorDefinition.ErrorDescription, context);
            }

            return (new Error(errorDefinition.Error, errorDescription), errorDefinition.StatusCode);
        }

        public int GetStatusCode(string code)
        {
            var errorDefinition = _errorCatalogue[code];
            if (errorDefinition == null)
            {
                return 400;
            }

            return errorDefinition.StatusCode;
        }

        public JsonResult GetErrorResponse(string code, string? context = null)
        {
            var errorDefinition = GetErrorDefinition(code);
            var errorDescription = errorDefinition.ErrorDescription;
            if (context != null)
            {
                errorDescription = string.Format(errorDefinition.ErrorDescription, context);
            }

            if (errorDefinition.IsCdsError)
            {
                var cdsError = new CdsError()
                {
                    Code = errorDefinition.Error,
                    Title = errorDefinition.ErrorTitle,
                    Detail = errorDescription,
                };
                return new JsonResult(new CdsErrorList(cdsError)) { StatusCode = errorDefinition.StatusCode };
            }

            var error = new Error(errorDefinition.Error, errorDescription);
            return new JsonResult(error) { StatusCode = errorDefinition.StatusCode };
        }

        public ValidationResult GetValidationResult(string code)
        {
            var (error, statusCode) = GetError(code);
            return ValidationResult.Fail(error.Code, error.Description, statusCode);
        }

        public ValidationResult GetValidationResult(string code, string context)
        {
            var (error, statusCode) = GetError(code, context);
            return ValidationResult.Fail(error.Code, error.Description, statusCode);
        }

        public class ErrorDefinition
        {
            public string Category { get; private set; }
            public string Code { get; private set; }
            public string Error { get; private set; }
            public string ErrorDescription { get; private set; }
            public string? ErrorTitle { get; private set; }
            public int StatusCode { get; private set; } = 400;
            public bool IsCdsError { get; private set; } = false;

            public ErrorDefinition(
                string category,
                string code,
                string error,
                string errorDescription,
                int statusCode,
                bool isCdsError = false,
                string? errorTitle = null)
            {
                this.Category = category;
                this.Code = code;
                this.Error = error;
                this.ErrorDescription = errorDescription;
                this.StatusCode = statusCode;
                this.IsCdsError = isCdsError;
                this.ErrorTitle = errorTitle;
            }
        }

    }
}
