using System.IdentityModel.Tokens.Jwt;
using CdrAuthServer.Configuration;
using CdrAuthServer.Exceptions;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.IdentityModel.Tokens;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    public class JwtValidator : IJwtValidator
    {
        private readonly ILogger<JwtValidator> _logger;
        private readonly IClientService _clientService;

        public JwtValidator(
            ILogger<JwtValidator> logger,
            IConfiguration configuration,
            IClientService clientService)
        {
            _logger = logger;
            _clientService = clientService;
        }

        public async Task<(ValidationResult, JwtSecurityToken?)> Validate(
            string jwt,
            Client client,
            JwtValidationContext context,
            ConfigurationOptions configOptions,
            IList<string>? validAudiences = null,
            IList<string>? validAlgorithms = null)
        {
            try
            {
                // Get the signing keys from the client's jwks endpoint.
                var signingKeys = await _clientService.GetSigningKeys(client);

                // Validate the jwt.
                var coreValidAudiences = new List<string> {
                    configOptions.Issuer,
                    configOptions.TokenEndpoint,
                    configOptions.PushedAuthorizationEndpoint,
                    configOptions.IntrospectionEndpoint,
                    configOptions.RevocationEndpoint,
                    configOptions.ArrangementRevocationEndpoint
                };

                if (validAudiences != null && validAudiences.Any())
                {
                    coreValidAudiences.Clear();
                    coreValidAudiences.AddRange(validAudiences);
                }

                var tokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKeys = signingKeys,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = client.ClientId,
                    ValidateIssuer = true,

                    ValidAudiences = coreValidAudiences,
                    ValidateAudience = true,

                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(configOptions.ClockSkewSeconds),

                    ValidAlgorithms = validAlgorithms ?? new List<string>() { Algorithms.Signing.PS256, Algorithms.Signing.ES256 }
                };

                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(jwt, tokenValidationParameters, out var token);

                return (ValidationResult.Pass(), (JwtSecurityToken)token);
            }
            catch (SecurityTokenInvalidAudienceException audException)
            {
                _logger.LogError(audException, "Invalid audience");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.JWT_INVALID_AUDIENCE, context.ToString()), null);
            }
            catch (SecurityTokenExpiredException expException)
            {
                _logger.LogError(expException, "JWT has expired");
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.JWT_EXPIRED, context.ToString()), null);
            }
            catch (JwksException jwksException)
            {
                _logger.LogError(jwksException, "Invalid {context} - jwks error", context);
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.JWKS_ERROR, context.ToString()), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid {context} - token validation error", context);
                return (ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.JWT_VALIDATION_ERROR, context.ToString()), null);
            }
        }
    }
}
