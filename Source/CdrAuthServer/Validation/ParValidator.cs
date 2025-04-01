using System.IdentityModel.Tokens.Jwt;
using CdrAuthServer.Configuration;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    public class ParValidator : IParValidator
    {
        private readonly ILogger<ParValidator> _logger;
        private readonly IJwtValidator _jwtValidator;
        private readonly IRequestObjectValidator _requestObjectValidator;
        private readonly IClientService _clientService;

        public ParValidator(
            ILogger<ParValidator> logger,
            IJwtValidator jwtValidator,
            IRequestObjectValidator requestObjectValidator,
            IClientService clientService)
        {
            _logger = logger;
            _jwtValidator = jwtValidator;
            _requestObjectValidator = requestObjectValidator;
            _clientService = clientService;
        }

        public async Task<(ValidationResult ValidationResult, AuthorizationRequestObject? AuthorizationRequestObject)> Validate(
            string clientId,
            string requestObject,
            ConfigurationOptions configOptions)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(requestObject))
            {
                _logger.LogError("request is not a well-formed JWT - {@RequestObject}", requestObject);
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.PAR_REQUEST_IS_NOT_WELL_FORMED_JWT), null);
            }

            var client = await _clientService.Get(clientId);
            if (client == null)
            {
                return (ValidationResult.Fail(ErrorCodes.Generic.InvalidClient, $"No client found for {clientId}"), null);
            }

            var (validationResult, requestJwt) = await _jwtValidator.Validate(
                requestObject,
                client,
                JwtValidationContext.Request,
                configOptions,
                validAudiences: [configOptions.Issuer, configOptions.PushedAuthorizationEndpoint],
                validAlgorithms: configOptions.RequestObjectSigningAlgValuesSupported);

            if (validationResult == null || !validationResult.IsValid || requestJwt == null)
            {
                _logger.LogError("request validation failed with error {@ErrorDescription}", validationResult);
                return (ValidationResult.Fail(ErrorCodes.Generic.InvalidRequestObject, $"{validationResult?.ErrorDescription}"), null);
            }

            // Perform additional validation on the request jwt.
            var (requestValidationResult, validatedRequestObject) = await _requestObjectValidator.Validate(clientId, requestJwt, configOptions);
            if (requestValidationResult != null && !requestValidationResult.IsValid)
            {
                _logger.LogError("additional request validation failed with error {ErrorDescription}", requestValidationResult.ErrorDescription);
                return (ValidationResult.Fail(requestValidationResult.Error ?? string.Empty, requestValidationResult.ErrorDescription, requestValidationResult.StatusCode.HasValue ? requestValidationResult.StatusCode.Value : 400), null);
            }

            return (ValidationResult.Pass(), validatedRequestObject);
        }
    }
}
