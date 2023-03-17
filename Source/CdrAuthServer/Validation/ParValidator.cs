using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using System.IdentityModel.Tokens.Jwt;
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

        public async Task<(ValidationResult, AuthorizationRequestObject?)> Validate(
            string clientId, 
            string requestObject, 
            ConfigurationOptions configOptions)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(requestObject))
            {
                _logger.LogError("request is not a well-formed JWT - {@requestObject}", requestObject);
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.PAR_REQUEST_IS_NOT_WELL_FORMED_JWT), null);
            }

            var client = await _clientService.Get(clientId);
            var (validationResult, requestJwt) = await _jwtValidator.Validate(
                requestObject, 
                client, 
                JwtValidationContext.request,
                configOptions,
                validAudiences: new List<string>() { configOptions.Issuer, configOptions.PushedAuthorizationEndpoint },
                validAlgorithms: configOptions.RequestObjectSigningAlgValuesSupported);

            if (validationResult == null || !validationResult.IsValid)
            {
                _logger.LogError("request validation failed with error {@validationResult.ErrorDescription}", validationResult);
                return (ValidationResult.Fail(ErrorCodes.InvalidRequestObject, $"{validationResult?.ErrorDescription}"), null);
            }

            // Perform additional validation on the request jwt.
            var (requestValidationResult, validatedRequestObject) = await _requestObjectValidator.Validate(clientId, requestJwt, configOptions);
            if (requestValidationResult != null && !requestValidationResult.IsValid)
            {
                _logger.LogError("additional request validation failed with error {@requestValidationResult.ErrorDescription}", requestValidationResult);
                return (ValidationResult.Fail(requestValidationResult.Error, requestValidationResult.ErrorDescription, requestValidationResult.StatusCode.HasValue ? requestValidationResult.StatusCode.Value : 400), null);
            }

            return (ValidationResult.Pass(), validatedRequestObject);
        }
    }
}
