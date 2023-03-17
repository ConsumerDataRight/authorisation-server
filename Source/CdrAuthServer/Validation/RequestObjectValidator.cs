using System.IdentityModel.Tokens.Jwt;
using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure.Comparers;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    public class RequestObjectValidator : IRequestObjectValidator
    {
        private readonly ILogger<RequestObjectValidator> _logger;
        private readonly AuthorizationRequestObject _validatedAuthorizationRequestObject;
        private readonly IClientService _clientService;
        private readonly IGrantService _grantService;

        public RequestObjectValidator(
            ILogger<RequestObjectValidator> logger,
            IClientService clientService,
            IGrantService grantService)
        {
            _logger = logger;
            _clientService = clientService;
            _grantService = grantService;
            _validatedAuthorizationRequestObject = new AuthorizationRequestObject();
        }

        public async Task<(ValidationResult, AuthorizationRequestObject?)> Validate(string clientId, JwtSecurityToken requestObject, ConfigurationOptions configOptions)
        {
            // Extract the client_id from the request object.
            if (!requestObject.Payload.TryGetValue(ClaimNames.ClientId, out var payloadClientId))
            {
                _logger.LogError("client_id missing from request object JWT");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ID_MISSING), null);
            }

            // Client Id must match.
            if (payloadClientId?.ToString()?.Equals(clientId, StringComparison.OrdinalIgnoreCase) is not true)
            {
                _logger.LogError("client_id does not match client_id in request object JWT");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REQUEST_OBJECT_JWT_CLIENT_ID_MISMATCH), null);
            }

            // Extract the redirect_uri from the request object.
            var redirectUri = requestObject.GetClaimValue(ClaimNames.RedirectUri);
            if (string.IsNullOrEmpty(redirectUri))
            {
                _logger.LogError("redirect_uri missing from request object JWT");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REQUEST_OBJECT_JWT_REDIRECT_URI_MISSING), null);
            }

            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var _))
            {
                _logger.LogError("malformed redirect_uri: {redirectUri}", redirectUri);
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_REDIRECT_URI), null);
            }

            // Check that the redirect_uri is valid.
            var client = await _clientService.Get(clientId);
            if (client == null || !client.RedirectUris.Contains(redirectUri.Split('?')[0]))
            {
                _logger.LogError("Invalid redirect_uri for client");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_REDIRECT_URI), null);
            }

            // Validate lifetime.
            var lifetimeResult = ValidateLifetime(requestObject);
            if (!lifetimeResult.IsValid)
            {
                _logger.LogError("Error validating request lifetime: {Error} {ErrorDescription}", lifetimeResult.Error, lifetimeResult.ErrorDescription);
                return (lifetimeResult, null);
            }

            if (requestObject.Payload.TryGetValue(ClaimNames.RequestUri, out var _))
            {
                _logger.LogError("request_uri is not supported in request object");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REQUEST_OBJECT_JWT_REQUEST_URI_NOT_SUPPORTED), null);
            }

            _validatedAuthorizationRequestObject.ClientId = clientId;
            _validatedAuthorizationRequestObject.RedirectUri = redirectUri;

            // state, response_type, response_mode
            var mandatoryResult = ValidateCoreParameters(requestObject, configOptions);
            if (!mandatoryResult.IsValid)
            {
                _logger.LogError("Error validating request core parameters: {Error} {ErrorDescription}", mandatoryResult.Error, mandatoryResult.ErrorDescription);
                return (mandatoryResult, null);
            }

            // Check that the client has registered (DCR) for the provided response type.
            if (!client.ResponseTypes.Contains(_validatedAuthorizationRequestObject.ResponseType))
            {
                var responseTypeError = ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.RESPONSE_TYPE_NOT_REGISTERED);
                _logger.LogError("Error validating request: {Error} {ErrorDescription}", responseTypeError.Error, responseTypeError.ErrorDescription);
                return (responseTypeError, null);
            }

            // scope, scope restrictions and plausability
            var scopeResult = ValidateScope(requestObject);
            if (!scopeResult.IsValid)
            {
                _logger.LogError("Error validating request scope parameters: {Error} {ErrorDescription}", scopeResult.Error, scopeResult.ErrorDescription);
                return (scopeResult, null);
            }

            // nonce, prompt, acr_values, login_hint etc.
            var optionalResult = ValidateOptionalParameters(requestObject);
            if (!optionalResult.IsValid)
            {
                _logger.LogError("Error validating request optional parameters: {Error} {ErrorDescription}", optionalResult.Error, optionalResult.ErrorDescription);
                return (optionalResult, null);
            }

            // CDR Arrangement Id
            var cdrArrangementIdResult = await ValidateCdrArrangementId(clientId, requestObject);
            if (!cdrArrangementIdResult.IsValid)
            {
                _logger.LogError("Error validating request cdr_arrangement_id parameter: {Error} {ErrorDescription}", cdrArrangementIdResult.Error, cdrArrangementIdResult.ErrorDescription);
                return (cdrArrangementIdResult, null);
            }

            return (ValidationResult.Pass(), _validatedAuthorizationRequestObject);
        }

        private ValidationResult ValidateLifetime(JwtSecurityToken requestObject)
        {
            var exp = requestObject.Payload.Exp.FromEpoch();
            var nbf = requestObject.Payload.Nbf.FromEpoch();

            // exp claim is required.
            if (exp == null)
            {
                _logger.LogError("Invalid request - exp is missing");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.EXP_MISSING);
            }

            // nbf claim is required.
            if (nbf == null)
            {   
                _logger.LogError("Invalid request - nbf is missing");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.NBF_MISSING);
            }

            // nbf cannot be after expiry
            // expiry cannot be longer than 60 min after not before 
            if (nbf.Value > exp.Value || (exp.Value.Subtract(nbf.Value) > TimeSpan.FromMinutes(60)))
            {   
                _logger.LogError("Invalid request - exp claim cannot be longer than 60 minutes after the nbf claim");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.EXPIRY_GREATER_THAN_60_AFTER_NBF);
            }

            return ValidationResult.Pass();
        }

        private ValidationResult ValidateCoreParameters(JwtSecurityToken requestObject, ConfigurationOptions configOptions)
        {
            var state = requestObject.GetClaimValue(ClaimNames.State);
            if (state != null && state.HasValue())
            {
                _validatedAuthorizationRequestObject.State = state;
            }

            //////////////////////////////////////////////////////////
            // response_type must be present and supported
            //////////////////////////////////////////////////////////
            var responseType = requestObject.GetClaimValue(ClaimNames.ResponseType);
            if (string.IsNullOrEmpty(responseType))
            {   
                _logger.LogError("response_type missing from request object JWT");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.RESPONSE_TYPE_MISSING);
            }

            // The responseType may come in in an unconventional order.
            // Use an IEqualityComparer that doesn't care about the order of multiple values.
            // Per https://tools.ietf.org/html/rfc6749#section-3.1.1 -
            // 'Extension response types MAY contain a space-delimited (%x20) list of
            // values, where the order of values does not matter (e.g., response
            // type "a b" is the same as "b a").'
            // http://openid.net/specs/oauth-v2-multiple-response-types-1_0-03.html#terminology -
            // 'If a response type contains one of more space characters (%20), it is compared
            // as a space-delimited list of values in which the order of values does not matter.'
            var comparer = new AnyOrderComparer();
            var supportedResponseType = configOptions.ResponseTypesSupported?
                .FirstOrDefault(x => comparer.Equals(x, responseType.ToString()));

            if (string.IsNullOrEmpty(supportedResponseType))
            {   
                _logger.LogError("response_type is not supported");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.RESPONSE_TYPE_NOT_SUPPORTED);
            }

            _validatedAuthorizationRequestObject.ResponseType = supportedResponseType;
            _validatedAuthorizationRequestObject.GrantType = (supportedResponseType == ResponseTypes.AuthCode) ? GrantTypes.AuthCode : GrantTypes.Hybrid;

            /////////////////////////////////////////////////////////////////////////////
            // validate code_challenge and code_challenge_method
            /////////////////////////////////////////////////////////////////////////////
            var proofKeyResult = ValidatePkceParameters(requestObject, configOptions);
            if (!proofKeyResult.IsValid)
            {
                return proofKeyResult;
            }

            //////////////////////////////////////////////////////////
            // check response_mode parameter and set response_mode
            //////////////////////////////////////////////////////////

            // check if response_mode parameter is present and valid
            var responseMode = requestObject.GetClaimValue(ClaimNames.ResponseMode) ?? GetDefaultResponseMode(_validatedAuthorizationRequestObject.ResponseType, configOptions);
            if (responseMode.HasValue())
            {
                if (configOptions.ResponseModesSupported?.Contains(responseMode) is not true)
                {   
                    _logger.LogError("Invalid response_mode");
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_RESPONSE_MODE);
                }

                if (!SupportedResponseModesForResponseType[_validatedAuthorizationRequestObject.ResponseType].Contains(responseMode))
                {   
                    _logger.LogError("Invalid response_mode for response_type");
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_RESPONSE_MODE_FOR_RESPONSE_TYPE);
                }

                // Extra validation for FAPI 1.0 Advanced.
                if (responseType.Equals(ResponseTypes.AuthCode) && !responseMode.Equals(ResponseModes.Jwt))
                {
                    _logger.LogError("response_mode not set to 'jwt' for response_type of 'code'");
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.MISSING_RESPONSE_MODE);
                }

                _validatedAuthorizationRequestObject.ResponseMode = (responseMode == ResponseModes.Jwt) ? GetDefaultResponseMode(_validatedAuthorizationRequestObject.ResponseType, configOptions) : responseMode;
            }
            else
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////
                // response_type is 'code' then response_mode of 'jwt' is required, as per FAPI 1.0 Advanced
                // https://openid.net/specs/openid-financial-api-part-2-1_0.html#advanced-security-provisions
                //
                // 2. shall require
                //    1. the response_type value code id_token, or
                //    2. the response_type value code in conjunction with the response_mode value jwt;
                ////////////////////////////////////////////////////////////////////////////////////////////////
                if (_validatedAuthorizationRequestObject.ResponseType == ResponseTypes.AuthCode)
                {
                    // Response mode of jwt is required.
                    _logger.LogError("response_mode is missing for response_type of 'code'");
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.MISSING_RESPONSE_MODE);
                }
            }

            return ValidationResult.Pass();
        }

        private string GetDefaultResponseMode(string responseType, ConfigurationOptions configOptions)
        {
            if (string.IsNullOrEmpty(responseType))
            {
                return configOptions.ResponseModesSupported.First();
            }

            return SupportedResponseModesForResponseType[responseType].First();
        }

        private ValidationResult ValidatePkceParameters(JwtSecurityToken requestObject, ConfigurationOptions configOptions)
        {
            var codeChallenge = requestObject.GetClaimValue(ClaimNames.CodeChallenge);
            if (string.IsNullOrEmpty(codeChallenge))
            {   
                _logger.LogError("code_challenge is missing");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CODE_CHALLENGE_MISSING);
            }

            if (codeChallenge.Length < ValidationRestrictions.InputLengthRestrictions.CodeChallengeMinLength ||
                codeChallenge.Length > ValidationRestrictions.InputLengthRestrictions.CodeChallengeMaxLength)
            {   
                _logger.LogError("Invalid code_challenge - invalid length");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CODE_CHALLENGE_INVALID_LENGTH);
            }

            var codeChallengeMethod = requestObject.GetClaimValue(ClaimNames.CodeChallengeMethod);
            if (string.IsNullOrEmpty(codeChallengeMethod))
            {
                _logger.LogDebug("Missing code_challenge_method, defaulting to S256");
                codeChallengeMethod = CodeChallengeMethods.S256;
            }

            if (configOptions.CodeChallengeMethodsSupported?.Contains(codeChallengeMethod) is not true)
            {   
                _logger.LogError("Unsupported code_challenge_method");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.UNSUPPORTED_CHALLENGE_METHOD);
            }

            _validatedAuthorizationRequestObject.CodeChallenge = codeChallenge;
            _validatedAuthorizationRequestObject.CodeChallengeMethod = codeChallengeMethod;

            return ValidationResult.Pass();
        }

        private ValidationResult ValidateScope(JwtSecurityToken requestObject)
        {
            //////////////////////////////////////////////////////////
            // scope must be present
            //////////////////////////////////////////////////////////
            var scope = requestObject.GetClaimValue(ClaimNames.Scope);
            if (scope.IsNullOrEmpty())
            {   
                _logger.LogError("Invalid scope - scope is missing");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.SCOPE_MISSING);
            }

            if (scope.Length > ValidationRestrictions.InputLengthRestrictions.ScopeMaxLength)
            {   
                _logger.LogError("Invalid scope - scope is too long");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.SCOPE_TOO_LONG);
            }

            _validatedAuthorizationRequestObject.Scope = scope;

            if (!_validatedAuthorizationRequestObject.Scopes.Contains(Scopes.OpenId))
            {   
                _logger.LogError("Missing openid scope");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.OPEN_ID_SCOPE_MISSING);
            }

            return ValidationResult.Pass();
        }

        private async Task<ValidationResult> ValidateCdrArrangementId(string clientId, JwtSecurityToken requestObject)
        {
            var claims = requestObject.Claims.FirstOrDefault(x => x.Type.Trim() == ClaimNames.Claims)?.Value;

            try
            {
                _validatedAuthorizationRequestObject.Claims = JsonConvert.DeserializeObject<AuthorizeClaims>(claims) ?? new AuthorizeClaims();
            }
            catch (Exception ex)
            {   
                _logger.LogError(ex, "Invalid claims in request object - {message}", ex.Message);
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_CLAIMS);
            }

            if (string.IsNullOrWhiteSpace(_validatedAuthorizationRequestObject.Claims.CdrArrangementId))
            {
                return ValidationResult.Pass();
            }

            var cdrArrangementGrant = await _grantService.Get(GrantTypes.CdrArrangement, _validatedAuthorizationRequestObject.Claims.CdrArrangementId, clientId);
            if (cdrArrangementGrant == null)
            {
                _logger.LogError("Invalid cdr_arrangement_id");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_CDR_ARRANGEMENT_ID);
            }

            return ValidationResult.Pass();
        }

        private ValidationResult ValidateOptionalParameters(JwtSecurityToken requestObject)
        {
            //////////////////////////////////////////////////////////
            // check nonce
            //////////////////////////////////////////////////////////
            var nonce = requestObject.GetClaimValue(ClaimNames.Nonce);
            if (nonce.IsNullOrEmpty())
            {   
                _logger.LogError("Invalid nonce");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_NONCE);
            }

            _validatedAuthorizationRequestObject.Nonce = nonce;

            //////////////////////////////////////////////////////////
            // check prompt
            //////////////////////////////////////////////////////////
            //var prompt = request.Raw.Get(OidcConstants.AuthorizeRequest.Prompt);
            //if (prompt.IsPresent())
            //{
            //    if (SupportedPromptModes.Contains(prompt))
            //    {
            //        request.PromptModes = new List<string>(new string[] { prompt });
            //    }
            //    else
            //    {
            //        _logger.LogDebug("Unsupported prompt mode - ignored: {prompt}", prompt);
            //    }
            //}

            //////////////////////////////////////////////////////////
            // check ui locales
            //////////////////////////////////////////////////////////
            //var uilocalesParameter = GetParameter(request, OidcConstants.AuthorizeRequest.UiLocales, _options.InputLengthRestrictions.UiLocale);
            //if (uilocalesParameter.Error != null)
            //{
            //    return uilocalesParameter.Error;
            //}
            //request.UiLocales = uilocalesParameter.Value;


            //////////////////////////////////////////////////////////
            // check max_age
            //////////////////////////////////////////////////////////
            //var maxAge = request.Raw.Get(OidcConstants.AuthorizeRequest.MaxAge);
            //if (maxAge.IsPresent())
            //{
            //    if (!int.TryParse(maxAge, out var seconds) || seconds < 0)
            //    {
            //        LogError("Invalid max_age.", request);
            //        return Invalid(request, description: "Invalid max_age");
            //    }

            //    request.MaxAge = seconds;
            //}

            //////////////////////////////////////////////////////////
            // check login_hint
            //////////////////////////////////////////////////////////
            //var loginHintParameter = GetParameter(request, OidcConstants.AuthorizeRequest.LoginHint, _options.InputLengthRestrictions.LoginHint);
            //if (loginHintParameter.Error != null)
            //{
            //    return loginHintParameter.Error;
            //}
            //request.LoginHint = loginHintParameter.Value;

            //////////////////////////////////////////////////////////
            // check acr_values
            //////////////////////////////////////////////////////////
            //var acrValuesParameter = GetParameter(request, OidcConstants.AuthorizeRequest.AcrValues, _options.InputLengthRestrictions.AcrValues);
            //if (acrValuesParameter.Error != null)
            //{
            //    return acrValuesParameter.Error;
            //}
            //request.AuthenticationContextReferenceClasses = acrValuesParameter.Value.FromSpaceSeparatedString().Distinct().ToList();

            return ValidationResult.Pass();
        }
    }
}
