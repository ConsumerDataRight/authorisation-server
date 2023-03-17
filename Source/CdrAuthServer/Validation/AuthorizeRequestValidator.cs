using CdrAuthServer.Configuration;
using CdrAuthServer.Domain;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

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
            if (string.IsNullOrEmpty(authRequest.client_id))
            {
                _logger.LogError("AuthorizeRequestValidationResult - client_id is missing");
                return ErrorResult(result, ErrorCatalogue.CLIENT_ID_MISSING);
            }

            var client = await _clientService.Get(authRequest.client_id);
            if (client == null)
            {
                _logger.LogError("AuthorizeRequestValidationResult - no client found: {client_id}", authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.INVALID_CLIENT_ID);
            }

            // Request URI validation.
            if (string.IsNullOrEmpty(authRequest.request_uri))
            {
                _logger.LogError("AuthorizeRequestValidationResult - request_uri is missing");
                return ErrorResult(result, ErrorCatalogue.REQUEST_URI_MISSING);
            }

            var requestUriGrant = await _grantService.Get(GrantTypes.RequestUri, authRequest.request_uri, authRequest.client_id) as RequestUriGrant;
            if (requestUriGrant == null)
            {
                _logger.LogError("AuthorizeRequestValidationResult - no requestUriGrant found for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.INVALID_REQUEST_URI);
            }

            result.ValidatedAuthorizationRequestObject = JsonConvert.DeserializeObject<AuthorizationRequestObject>(requestUriGrant.Request ?? string.Empty) ?? new AuthorizationRequestObject();

            // Make sure client_id matches client_id in request object.
            if (!string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.ClientId) && authRequest.client_id != result.ValidatedAuthorizationRequestObject.ClientId)
            {
                _logger.LogError("AuthorizeRequestValidationResult - client_id does not match request_uri client_id for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.REQUEST_URI_CLIENT_ID_MISMATCH);
            }

            //
            // Validate and set the Redirect URI based on parameters, request object takes precedence.
            //
            var redirectUri = result.ValidatedAuthorizationRequestObject.RedirectUri ?? authRequest.redirect_uri;

            // Validate the redirect_uri so that errors can be returned to the client.
            if (string.IsNullOrEmpty(redirectUri))
            {
                // Set redirect uri to what is registered for the client.
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
                    _logger.LogError("AuthorizeRequestValidationResult - Invalid redirect_uri for client for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                    return ErrorResult(result, ErrorCatalogue.INVALID_REDIRECT_URI);
                }
            }

            // Set the Redirect URI to use for this authorization.
            result.ValidatedAuthorizationRequestObject.RedirectUri = redirectUri;

            // Check if the request uri has expired.
            if (checkGrantExpiredOrUsed && requestUriGrant.IsExpired)
            {
                _logger.LogError("AuthorizeRequestValidationResult - request_uri has expired for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.REQUEST_URI_EXPIRED);
            }

            // Check if the request uri has already been used.
            if (checkGrantExpiredOrUsed && requestUriGrant.UsedAt != null)
            {
                _logger.LogError("AuthorizeRequestValidationResult - request_uri has already been used for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.REQUEST_URI_ALREADY_USED);
            }

            //
            // Response type validation.
            //
            result.ValidatedAuthorizationRequestObject.ResponseType = result.ValidatedAuthorizationRequestObject.ResponseType ?? authRequest.response_type;
            _logger.LogDebug("AuthorizeRequestValidator: response_type = {responseType}", result.ValidatedAuthorizationRequestObject.ResponseType);
            if (string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.ResponseType))
            {
                _logger.LogError("AuthorizeRequestValidationResult - response_type is missing for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.RESPONSE_TYPE_MISSING);
            }

            if (configOptions.ResponseTypesSupported?.Contains(result.ValidatedAuthorizationRequestObject.ResponseType) is not true)
            {
                _logger.LogError("AuthorizeRequestValidationResult - response_type is not supported for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.RESPONSE_TYPE_NOT_SUPPORTED);
            }

            // Make sure response_type matches response_type in request object.
            if (!string.IsNullOrEmpty(authRequest.response_type)
             && !string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.ResponseType)
             && authRequest.response_type != result.ValidatedAuthorizationRequestObject.ResponseType)
            {
                _logger.LogError("AuthorizeRequestValidationResult - response_type does not match request_uri response_type for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.RESPONSE_TYPE_MISMATCH_REQUEST_URI_RESPONSE_TYPE);
            }

            // Scope validation.
            result.ValidatedAuthorizationRequestObject.Scope = result.ValidatedAuthorizationRequestObject.Scope ?? authRequest.scope;
            _logger.LogDebug("AuthorizeRequestValidator: scope = {scope}", result.ValidatedAuthorizationRequestObject.Scope);

            if (string.IsNullOrEmpty(result.ValidatedAuthorizationRequestObject.Scope))
            {
                _logger.LogError("AuthorizeRequestValidationResult - scope is missing for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.SCOPE_MISSING);
            }

            var scopes = result.ValidatedAuthorizationRequestObject.Scope.Split(' ');
            if (!scopes.Contains(Scopes.OpenId))
            {
                _logger.LogError("AuthorizeRequestValidationResult - openid scope is missing for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                return ErrorResult(result, ErrorCatalogue.OPEN_ID_SCOPE_MISSING);
            }

            // Response mode validation.
            var responseMode = result.ValidatedAuthorizationRequestObject.ResponseMode ?? authRequest.response_mode;
            if (!string.IsNullOrEmpty(responseMode))
            {
                var validResponseModes = Constants.SupportedResponseModesForResponseType[result.ValidatedAuthorizationRequestObject.ResponseType];
                if (!validResponseModes.Contains(responseMode))
                {
                    _logger.LogError("AuthorizeRequestValidationResult - response_mode is not valid for the response_type for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                    return ErrorResult(result, ErrorCatalogue.INVALID_RESPONSE_MODE);
                }
            }

            result.ValidatedAuthorizationRequestObject.ResponseMode = EnsureResponseMode(responseMode, result.ValidatedAuthorizationRequestObject.ResponseType);

            // Check software product status (if configured).
            if (configOptions.CdrRegister.CheckSoftwareProductStatus)
            {
                var softwareProduct = await _cdrService.GetSoftwareProduct(client.SoftwareId);
                if (softwareProduct == null)
                {
                    _logger.LogError("AuthorizeRequestValidationResult - Software product not found for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                    return ErrorResult(result, ErrorCatalogue.SOFTWARE_PRODUCT_NOT_FOUND);
                }
                if (!softwareProduct.IsActive())
                {
                    _logger.LogError("AuthorizeRequestValidationResult - Software product status is not active for uri:{request_uri} clientid:{client_id}", authRequest.request_uri, authRequest.client_id);
                    return ErrorResult(result, ErrorCatalogue.SOFTWARE_PRODUCT_STATUS_INACTIVE, softwareProduct.GetStatusDescription());
                }
            }

            // Set optional and default values.
            result.ValidatedAuthorizationRequestObject.Nonce = result.ValidatedAuthorizationRequestObject.Nonce ?? authRequest.nonce;
            result.ValidatedAuthorizationRequestObject.Scope = result.ValidatedAuthorizationRequestObject.Scope ?? authRequest.scope;

            result.IsValid = true;
            return result;
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

        private AuthorizeRequestValidationResult ErrorResult(
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
