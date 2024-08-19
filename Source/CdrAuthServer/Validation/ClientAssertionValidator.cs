using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using System.IdentityModel.Tokens.Jwt;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    public class ClientAssertionValidator : IClientAssertionValidator
    {
        private readonly ILogger<ClientAssertionValidator> _logger;
        private readonly IClientService _clientService;
        private readonly ITokenService _tokenService;
        private readonly IJwtValidator _jwtValidator;

        public ClientAssertionValidator(
            ILogger<ClientAssertionValidator> logger,
            IClientService clientService,
            ITokenService tokenService,
            IJwtValidator jwtValidator)
        {
            _logger = logger;
            _clientService = clientService;
            _tokenService = tokenService;
            _jwtValidator = jwtValidator;
        }

        public async Task<(ValidationResult, string? clientId)> ValidateClientAssertionRequest(
            ClientAssertionRequest clientAssertionRequest,
            ConfigurationOptions configOptions,
            bool isTokenEndpoint)
        {
            // Basic validation.

            // TODO: this is in the consumer data standards but is not valid as per the normative standards.
            // FAPI conformance testing will not pass the client_id in the request, thus we will not pass FAPI testing 
            // if this validation is in place.
            //if (string.IsNullOrEmpty(clientAssertion.client_id))
            //{
            //    return (false, "invalid_client", "client_id not provided", null);
            //}

            if (string.IsNullOrEmpty(clientAssertionRequest.ClientAssertion))
            {
                _logger.LogError("client_assertion not provided");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_NOT_PROVIDED), null);
            }

            if (string.IsNullOrEmpty(clientAssertionRequest.ClientAssertionType))
            {
                _logger.LogError("client_assertion_type not provided");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_TYPE_NOT_PROVIDED), null);
            }

            // Client assertion type needs to be urn:ietf:params:oauth:client-assertion-type:jwt-bearer.
            if (!clientAssertionRequest.ClientAssertionType.Equals("urn:ietf:params:oauth:client-assertion-type:jwt-bearer", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_CLIENT_ASSERTION_TYPE), null);
            }

            if (isTokenEndpoint)
            {
                if (string.IsNullOrEmpty(clientAssertionRequest.GrantType))
                {
                    _logger.LogError("grant_type not provided");
                    return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.GRANT_TYPE_NOT_PROVIDED), null);
                }

                // Grant type needs to be in the supported list.
                if (configOptions?.GrantTypesSupported?.Contains(clientAssertionRequest.GrantType) is not true)
                {
                    _logger.LogError("unsupported grant_type");
                    return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.UNSUPPORTED_GRANT_TYPE), null);
                }
            }

            // Validate the client assertion.
            List<string>? validAudiences = null;
            if (isTokenEndpoint)
            {
                validAudiences = new List<string>();
                validAudiences.Add(configOptions.Issuer);
                validAudiences.Add(configOptions.TokenEndpoint);
            }

            var (clientAssertionResult, client) = await ValidateClientAssertion(clientAssertionRequest.ClientAssertion, configOptions, validAudiences);
            if (!clientAssertionResult.IsValid)
            {
                _logger.LogError("clientAssertion failed with error - {ErrorDescription}", clientAssertionResult.ErrorDescription);
                return (ValidationResult.Fail(ErrorCodes.Generic.InvalidClient, clientAssertionResult.ErrorDescription), null);
            }

            // If client id was provided, then make sure it matches the client id in the client assertion.
            if (!string.IsNullOrEmpty(clientAssertionRequest.ClientId)
             && !clientAssertionRequest.ClientId.Equals(client?.ClientId))
            {
                _logger.LogError("client_id does not match client_assertion");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_CLIENT_ID_MISMATCH), null);
            }

            return (ValidationResult.Pass(), client?.ClientId);
        }

        public async Task<(ValidationResult, Client?)> ValidateClientAssertion(
            string clientAssertion,
            ConfigurationOptions configOptions,
            IList<string>? validAudiences = null)
        {
            // The issuer of the client assertion is the client_id of the calling data recipient.
            // Need to extract the client_id (iss) from client assertion to load the client details.
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(clientAssertion))
            {
                _logger.LogError("Cannot read client_assertion.  Invalid format.");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_INVALID_FORMAT), null);
            }

            var unvalidatedToken = handler.ReadJwtToken(clientAssertion);
            if (unvalidatedToken == null)
            {
                _logger.LogError("Cannot read client_assertion");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_NOT_READABLE), null);
            }

            var clientId = unvalidatedToken.Issuer;
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Missing iss claim");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_MISSING_ISS_CLAIM), null);
            }

            // Load the client.
            var client = await _clientService.Get(clientId);
            if (client == null)
            {
                _logger.LogError("Client not found - {clientId}", clientId);
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_NOT_FOUND), null);
            }

            var (validationResult, jwtToken) = await _jwtValidator.Validate(
                clientAssertion,
                client,
                JwtValidationContext.client_assertion,
                configOptions,
                validAudiences: validAudiences);

            if (!validationResult.IsValid)
            {
                return (validationResult, null);
            }

            if (string.IsNullOrEmpty(jwtToken?.Id))
            {
                _logger.LogError("Invalid client_assertion - 'jti' is required");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.JTI_REQUIRED), null);
            }

            if (await _tokenService.IsTokenBlacklisted(jwtToken.Id))
            {
                _logger.LogError("Invalid client_assertion - 'jti' must be unique");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.JTI_NOT_UNIQUE), null);
            }

            if (!client.ClientId.Equals(jwtToken.Subject, StringComparison.OrdinalIgnoreCase)
             || !client.ClientId.Equals(jwtToken.Issuer, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Invalid client_assertion - 'sub' and 'iss' must be set to the client_id");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_SUBJECT_ISS_NOT_SET_TO_CLIENT_ID), null);
            }

            if (!jwtToken.Subject.Equals(jwtToken.Issuer, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Invalid client_assertion - 'sub' and 'iss' must have the same value");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ASSERTION_SUBJECT_ISS_NOT_SAME_VALUE), null);
            }

            return (ValidationResult.Pass(), client);
        }
    }
}
