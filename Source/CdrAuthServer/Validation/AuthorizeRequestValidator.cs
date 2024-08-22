using CdrAuthServer.Configuration;
using CdrAuthServer.Domain;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;
using static CdrAuthServer.Infrastructure.Constants;

namespace CdrAuthServer.Validation
{
    public class AuthorizeRequestValidator : IAuthorizeRequestValidator
    {
        private readonly ILogger<AuthorizeRequestValidator> _logger;
        private readonly IClientService _clientService;
        private readonly IGrantService _grantService;
        private readonly ICdrService _cdrService;

        public AuthorizeRequestValidator(
            ILogger<AuthorizeRequestValidator> logger,
            IClientService clientService,
            IGrantService grantService,
            ICdrService cdrService)
        {
            _logger = logger;
            _clientService = clientService;
            _grantService = grantService;
            _cdrService = cdrService;
        }

        /// <summary>
        /// Validates an auth request using the requestObject (from PAR endpoint) and
        /// the parameters passed directly to the auth endpoint (authRequest).
        /// </summary>
        /// <param name="authRequest">Parameters received on authorization endpoint</param>
        /// <param name="configOptions">configuration</param>
        /// <param name="checkGrantExpiredOrUsed">checkGrantExpiredOrUsed</param>
        /// <returns>
        /// AuthorizeRequestValidationResult containing a validated auth request object.
        /// </returns>
        /// <remarks>
        /// - The requestObject values take precedence over the authRequest values.
        /// - client_id and response_type must match the values in the request object.
        /// </remarks>
        public async Task<AuthorizeRequestValidationResult> Validate(
            AuthorizeRequest authRequest,
            ConfigurationOptions configOptions,
            bool checkGrantExpiredOrUsed = true)
        {
            var result = new AuthorizeRequestValidationResult(false);

            // Client validation.
            if (!await ValidateClient(authRequest, result)) return result;
            var client = await _clientService.Get(authRequest.client_id);
            
            if (client == null) return ErrorResult(result, ErrorCatalogue.CLIENT_NOT_FOUND);

            // Request URI validation.
            if (!await ValidateRequestUri(authRequest, result, checkGrantExpiredOrUsed)) return result;

            // Validate and set the Redirect URI based on parameters, request object takes precedence.
            if (!ValidateRedirectUri(authRequest, result, client)) return result;

            // Response type validation.
            if (!ValidateResponseType(authRequest, result, configOptions)) return result;

            // Scope validation.
            if (!ValidateScope(authRequest, result)) return result;

            // Response mode validation.
            if (!ValidateResponseMode(authRequest, result)) return result;

            // Check software product status (if configured).
            if (!await ValidateSoftwareProduct(authRequest, result, client, configOptions)) return result;

            SetOptionalAndDefaultValues(authRequest, result);

            result.IsValid = true;
            return result;
        }

        private async Task<bool> ValidateClient(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result)
        {
            if (string.IsNullOrEmpty(authRequest.client_id))
            {
                _logger.LogError("AuthorizeRequestValidationResult - client_id is missing");
                return SetErrorResult(result, ErrorCatalogue.CLIENT_ID_MISSING);
            }

            var client = await _clientService.Get(authRequest.client_id);
            if (client == null)
            {
                _logger.LogError("AuthorizeRequestValidationResult - no client found: {Client_id}", authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.INVALID_CLIENT_ID);
            }

            return true;
        }

        private async Task<bool> ValidateRequestUri(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result, bool checkGrantExpiredOrUsed)
        {
            if (string.IsNullOrEmpty(authRequest.request_uri))
            {
                _logger.LogError("AuthorizeRequestValidationResult - request_uri is missing");
                return SetErrorResult(result, ErrorCatalogue.REQUEST_URI_MISSING);
            }

            var requestUriGrant = await _grantService.Get(GrantTypes.RequestUri, authRequest.request_uri, authRequest.client_id) as RequestUriGrant;
            if (requestUriGrant == null)
            {
                _logger.LogError("AuthorizeRequestValidationResult - no requestUriGrant found for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.INVALID_REQUEST_URI);
            }

            result.ValidatedAuthorizationRequestObject = JsonConvert.DeserializeObject<AuthorizationRequestObject>(requestUriGrant.Request ?? string.Empty) ?? new AuthorizationRequestObject();

            // Make sure client_id matches client_id in request object.
            if (!string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.ClientId) && authRequest.client_id != result.ValidatedAuthorizationRequestObject.ClientId)
            {
                _logger.LogError("AuthorizeRequestValidationResult - client_id does not match request_uri client_id for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.REQUEST_URI_CLIENT_ID_MISMATCH);
            }

            // Check if the request uri has expired.
            if (checkGrantExpiredOrUsed && requestUriGrant.IsExpired)
            {
                _logger.LogError("AuthorizeRequestValidationResult - request_uri has expired for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.REQUEST_URI_EXPIRED);
            }

            // Check if the request uri has already been used.
            if (checkGrantExpiredOrUsed && requestUriGrant.UsedAt != null)
            {
                _logger.LogError("AuthorizeRequestValidationResult - request_uri has already been used for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.REQUEST_URI_ALREADY_USED);
            }

            return true;
        }

        private bool ValidateRedirectUri(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result, Client client)
        {
            var redirectUri = result.ValidatedAuthorizationRequestObject.RedirectUri ?? authRequest.redirect_uri;

            if (string.IsNullOrEmpty(redirectUri))
            {
                if (client.RedirectUris != null && client.RedirectUris.Any())
                {
                    redirectUri = client.RedirectUris.First();
                }
            }
            else
            {
                // Remove query string from validation.
                var testRedirectUri = redirectUri.Split('?')[0];

                // Redirect URI validation.
                if (!client.RedirectUris.Contains(testRedirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogError("AuthorizeRequestValidationResult - Invalid redirect_uri for client for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                    return SetErrorResult(result, ErrorCatalogue.INVALID_REDIRECT_URI);
                }
            }

            // Set the Redirect URI to use for this authorization.
            result.ValidatedAuthorizationRequestObject.RedirectUri = redirectUri;
            return true;
        }

        private bool ValidateResponseType(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result, ConfigurationOptions configOptions)
        {
            result.ValidatedAuthorizationRequestObject.ResponseType = result.ValidatedAuthorizationRequestObject.ResponseType ?? authRequest.response_type;
            _logger.LogDebug("AuthorizeRequestValidator: response_type = {ResponseType}", result.ValidatedAuthorizationRequestObject.ResponseType);

            if (string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.ResponseType))
            {
                _logger.LogError("AuthorizeRequestValidationResult - response_type is missing for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.RESPONSE_TYPE_MISSING);
            }

            if (configOptions.ResponseTypesSupported?.Contains(result.ValidatedAuthorizationRequestObject.ResponseType) is not true)
            {
                _logger.LogError("AuthorizeRequestValidationResult - response_type is not supported for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.RESPONSE_TYPE_NOT_SUPPORTED);
            }

            if (!string.IsNullOrEmpty(authRequest.response_type)
             && !string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.ResponseType)
             && authRequest.response_type != result.ValidatedAuthorizationRequestObject.ResponseType)
            {
                _logger.LogError("AuthorizeRequestValidationResult - response_type does not match request_uri response_type for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.RESPONSE_TYPE_MISMATCH_REQUEST_URI_RESPONSE_TYPE);
            }

            return true;
        }

        private bool ValidateScope(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result)
        {
            result.ValidatedAuthorizationRequestObject.Scope = result.ValidatedAuthorizationRequestObject.Scope ?? authRequest.scope;
            _logger.LogDebug("AuthorizeRequestValidator: scope = {Scope}", result.ValidatedAuthorizationRequestObject.Scope);

            if (string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.Scope))
            {
                _logger.LogError("AuthorizeRequestValidationResult - scope is missing for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.SCOPE_MISSING);
            }

            var scopes = result.ValidatedAuthorizationRequestObject.Scope.Split(' ');
            if (!scopes.Contains(Scopes.OpenId))
            {
                _logger.LogError("AuthorizeRequestValidationResult - openid scope is missing for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                return SetErrorResult(result, ErrorCatalogue.OPEN_ID_SCOPE_MISSING);
            }

            return true;
        }

        private bool ValidateResponseMode(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result)
        {
            var responseMode = result.ValidatedAuthorizationRequestObject.ResponseMode ?? authRequest.response_mode;

            if (!string.IsNullOrEmpty(responseMode))
            {
                var validResponseModes = Constants.SupportedResponseModesForResponseType[result.ValidatedAuthorizationRequestObject.ResponseType];
                if (!validResponseModes.Contains(responseMode))
                {
                    _logger.LogError("AuthorizeRequestValidationResult - response_mode is not valid for the response_type for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                    return SetErrorResult(result, ErrorCatalogue.INVALID_RESPONSE_MODE);
                }
            }

            result.ValidatedAuthorizationRequestObject.ResponseMode = EnsureResponseMode(responseMode, result.ValidatedAuthorizationRequestObject.ResponseType);
            return true;
        }

        private async Task<bool> ValidateSoftwareProduct(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result, Client client, ConfigurationOptions configOptions)
        {
            if (configOptions.CdrRegister != null && configOptions.CdrRegister.CheckSoftwareProductStatus)
            {
                var softwareProduct = await _cdrService.GetSoftwareProduct(client.SoftwareId);
                if (softwareProduct == null)
                {
                    _logger.LogError("AuthorizeRequestValidationResult - Software product not found for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                    return SetErrorResult(result, ErrorCatalogue.SOFTWARE_PRODUCT_NOT_FOUND);
                }
                if (!softwareProduct.IsActive())
                {
                    _logger.LogError("AuthorizeRequestValidationResult - Software product status is not active for uri:{Request_uri} clientid:{Client_id}", authRequest.request_uri, authRequest.client_id);
                    return SetErrorResult(result, ErrorCatalogue.SOFTWARE_PRODUCT_STATUS_INACTIVE, softwareProduct.GetStatusDescription());
                }
            }

            return true;
        }

        private static void SetOptionalAndDefaultValues(AuthorizeRequest authRequest, AuthorizeRequestValidationResult result)
        {
            result.ValidatedAuthorizationRequestObject.Nonce = result.ValidatedAuthorizationRequestObject.Nonce ?? authRequest.nonce;
            result.ValidatedAuthorizationRequestObject.Scope = result.ValidatedAuthorizationRequestObject.Scope ?? authRequest.scope;
        }

        private static bool SetErrorResult(AuthorizeRequestValidationResult result, string errorCode, string? context = null)
        {
            var (error, _) = ErrorCatalogue.Catalogue().GetError(errorCode, context);
            result.IsValid = false;
            result.Error = error.Code;
            result.ErrorDescription = error.Description;
            return false;
        }


        public AuthorizeRequestValidationResult ValidateCallback(AuthorizeRequestValidationResult currentResult, AuthorizeCallbackRequest authCallbackRequest)
        {
            // If there are existing errors, we don't want to add more errors.
            if (!currentResult.IsValid)
            {
                return currentResult;
            }

            if (!string.IsNullOrEmpty(authCallbackRequest.error_code))
            {
                return ErrorResult(currentResult, authCallbackRequest.error_code);
            }

            return currentResult;
        }

        private static AuthorizeRequestValidationResult ErrorResult(
            AuthorizeRequestValidationResult result,
            string errorCode,
            string? context = null)
        {
            var (error, _) = ErrorCatalogue.Catalogue().GetError(errorCode, context);
            result.Error = error.Code;
            result.ErrorDescription = error.Description;
            result.IsValid = false;
            return result;
        }

        private static string EnsureResponseMode(string responseMode, string responseType)
        {
            if (string.IsNullOrEmpty(responseMode))
            {
                // Set default based on the response type.
                return SupportedResponseModesForResponseType[responseType][0];
            }

            // Default for "jwt" is "query.jwt".
            if (responseMode == ResponseModes.Jwt)
            {
                return ResponseModes.QueryJwt;
            }

            return responseMode;
        }
    }
}
