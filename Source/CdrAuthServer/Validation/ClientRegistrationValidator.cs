using CdrAuthServer.Configuration;
using CdrAuthServer.Domain;
using CdrAuthServer.Extensions;
using CdrAuthServer.Helpers;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    public class ClientRegistrationValidator : IClientRegistrationValidator
    {
        private readonly ILogger<ClientRegistrationValidator> _logger;
        private readonly IConfiguration _configuration;
        private readonly IJwksService _jwksService;
        private readonly List<System.ComponentModel.DataAnnotations.ValidationResult> _validationResults = new();
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientRegistrationValidator(
            IConfiguration configuration,
            ILogger<ClientRegistrationValidator> logger,
            IJwksService jwksService,
            IHttpContextAccessor httpContextAccessor)
        {
            // Request processing:
            // 1. Decode the request JWT received from Data Recipient, without validating the signature.
            // 2. Extract the software statement from the decoded request JWT.
            // 3. Validate the software statement JWT with the CDR Registry JWKS endpoint.
            // 4. Extract the ADR software JWKS endpoint from the software statement.
            // 5. Validate the request JWT using the ADR software JWKS endpoint extracted.
            _configuration = configuration;
            _logger = logger;
            _jwksService = jwksService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ValidationResult> Validate(ClientRegistrationRequest clientRegistrationRequest, ConfigurationOptions configOptions)
        {
            if (clientRegistrationRequest == null)
            {
                _logger.LogError("Client Registration Request is null");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.EMPTY_REGISTRATION_REQUEST);
            }

            // 1. SSA validation first.  If it fails, then exit as no point in validating anything else.
            var ssaResult = await ValidateSSA(clientRegistrationRequest, configOptions);
            if (!ssaResult.IsValid)
            {
                _logger.LogError("SSA validation failed: {Error} {ErrorDescription}", ssaResult.Error, ssaResult.ErrorDescription);
                return ssaResult;
            }

            // 2. Signature validation to determine if we can rely on the contents of the registration request jwt.
            var signatureResult = await ValidateRequestSignature(clientRegistrationRequest, configOptions);
            if (!signatureResult.IsValid)
            {
                _logger.LogError("Client Registration Signature validation failed: {Error} {ErrorDescription}", signatureResult.Error, signatureResult.ErrorDescription);
                return signatureResult;
            }

            // 3. Validate the sector identifier uri
            var sectorIdentifierResult = await ValidateSectorIdentifierUri(clientRegistrationRequest.SoftwareStatement.SectorIdentifierUri);
            if (!sectorIdentifierResult.IsValid)
            {
                _logger.LogError("Sector Identifier validation failed: {Error} {ErrorDescription}", sectorIdentifierResult.Error, sectorIdentifierResult.ErrorDescription);
                return sectorIdentifierResult;
            }

            //
            // Signature validation has been completed successfully.
            //

            // 4. Basic validation.
            CheckMandatory(clientRegistrationRequest.Iss, nameof(clientRegistrationRequest.Iss));
            CheckMandatory(clientRegistrationRequest.Iat, nameof(clientRegistrationRequest.Iat));
            CheckMandatory(clientRegistrationRequest.Exp, nameof(clientRegistrationRequest.Exp));
            CheckMandatory(clientRegistrationRequest.Jti, nameof(clientRegistrationRequest.Jti));
            MustEqual(clientRegistrationRequest.Aud, nameof(clientRegistrationRequest.Aud), configOptions.Issuer);
            MustEqual(clientRegistrationRequest.Iss, nameof(clientRegistrationRequest.Iss), clientRegistrationRequest.SoftwareStatement.SoftwareId!);
            MustBeOne(clientRegistrationRequest.TokenEndpointAuthSigningAlg, nameof(clientRegistrationRequest.TokenEndpointAuthSigningAlg), configOptions.TokenEndpointAuthSigningAlgValuesSupported!);
            MustBeOne(clientRegistrationRequest.TokenEndpointAuthMethod, nameof(clientRegistrationRequest.TokenEndpointAuthMethod), configOptions.TokenEndpointAuthMethodsSupported!);
            MustBeOne(clientRegistrationRequest.IdTokenSignedResponseAlg, nameof(clientRegistrationRequest.IdTokenSignedResponseAlg), configOptions.IdTokenSigningAlgValuesSupported!);
            MustContain(clientRegistrationRequest.GrantTypes, nameof(clientRegistrationRequest.GrantTypes), "authorization_code");

            if (!string.IsNullOrEmpty(clientRegistrationRequest.RequestObjectSigningAlg))
            {
                MustBeOne(clientRegistrationRequest.RequestObjectSigningAlg, nameof(clientRegistrationRequest.RequestObjectSigningAlg), configOptions.RequestObjectSigningAlgValuesSupported!);
            }

            // Response types is mandatory.
            if (clientRegistrationRequest.ResponseTypes == null || !clientRegistrationRequest.ResponseTypes.Any())
            {
                _validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult(string.Format(Constants.ValidationErrorMessages.MissingClaim, GetDisplayName(nameof(clientRegistrationRequest.ResponseTypes))), new string[] { nameof(clientRegistrationRequest.ResponseTypes) }));
            }
            else
            {
                foreach (var responseType in clientRegistrationRequest.ResponseTypes)
                {
                    MustBeOne(responseType, nameof(clientRegistrationRequest.ResponseTypes), configOptions.ResponseTypesSupported!);
                }

                if (clientRegistrationRequest.ResponseTypes.Contains(ResponseTypes.AuthCode))
                {
                    MustBeOne(clientRegistrationRequest.AuthorizationSignedResponseAlg, nameof(clientRegistrationRequest.AuthorizationSignedResponseAlg), configOptions.AuthorizationSigningAlgValuesSupported!);
                }

                if (clientRegistrationRequest.ResponseTypes.Contains(ResponseTypes.Hybrid))
                {
                    MustBeOne(clientRegistrationRequest.IdTokenEncryptedResponseAlg, nameof(clientRegistrationRequest.IdTokenEncryptedResponseAlg), configOptions.IdTokenEncryptionAlgValuesSupported!);
                    MustBeOne(clientRegistrationRequest.IdTokenEncryptedResponseEnc, nameof(clientRegistrationRequest.IdTokenEncryptedResponseEnc), configOptions.IdTokenEncryptionEncValuesSupported!);
                }
            }

            if (!string.IsNullOrEmpty(clientRegistrationRequest.AuthorizationEncryptedResponseEnc))
            {
                MustBeOne(clientRegistrationRequest.AuthorizationEncryptedResponseEnc, nameof(clientRegistrationRequest.AuthorizationEncryptedResponseEnc), configOptions.AuthorizationEncryptionEncValuesSupportedList!);
                MustBeOne(clientRegistrationRequest.AuthorizationEncryptedResponseAlg, nameof(clientRegistrationRequest.AuthorizationEncryptedResponseAlg), configOptions.AuthorizationEncryptionAlgValuesSupportedList!);
            }

            if (!string.IsNullOrEmpty(clientRegistrationRequest.AuthorizationEncryptedResponseAlg))
            {
                MustBeOne(clientRegistrationRequest.AuthorizationEncryptedResponseAlg, nameof(clientRegistrationRequest.AuthorizationEncryptedResponseAlg), configOptions.AuthorizationEncryptionAlgValuesSupportedList!);
            }

            if (!string.IsNullOrEmpty(clientRegistrationRequest.ApplicationType))
            {
                MustEqual(clientRegistrationRequest.ApplicationType, nameof(clientRegistrationRequest.ApplicationType), "web");
            }

            // redirect_uri validation.
            foreach (var redirectUri in clientRegistrationRequest.RedirectUris)
            {
                if (!clientRegistrationRequest.SoftwareStatement.RedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogError("redirect_uri: {RedirectUri} is not present in SoftwareStatement", redirectUri);
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REGISTRATION_REQUEST_INVALID_REDIRECT_URI, redirectUri);
                }

                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var _))
                {
                    _logger.LogError("malformed redirect_uri: {RedirectUri}", redirectUri);
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_REDIRECT_URI);
                }
            }

            if (_validationResults.Count != 0)
            {
                _logger.LogError("validation failed: {@ValidationResults}", _validationResults);
                return ValidationResult.Fail(ErrorCodes.Generic.InvalidClientMetadata, _validationResults);
            }

            return ValidationResult.Pass();
        }

        private bool CheckMandatory(object? propValue, string propName)
        {
            if (propValue == null || (propValue as string) == "")
            {
                _validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult(string.Format(Constants.ValidationErrorMessages.MissingClaim, GetDisplayName(propName)), [propName]));
                return false;
            }

            return true;
        }

        private bool MustBeOne(object? propValue, string propName, IEnumerable<string> expectedValues, string? customErrorMessage = null)
        {
            if (!CheckMandatory(propValue, propName))
            {
                return false;
            }

            if (!expectedValues.Contains(propValue?.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(customErrorMessage))
                {
                    _validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult(string.Format(Constants.ValidationErrorMessages.MustBeOne, GetDisplayName(propName), String.Join(",", expectedValues)), [propName]));
                }
                else
                {
                    _validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult(customErrorMessage, [propName]));
                }

                return false;
            }

            return true;
        }

        private void MustEqual(object? propValue, string propName, string expectedValue)
        {
            if (!CheckMandatory(propValue, propName))
            {
                return;
            }

            if (!propValue?.ToString()?.Equals(expectedValue, StringComparison.OrdinalIgnoreCase) is true)
            {
                _validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult(string.Format(Constants.ValidationErrorMessages.MustEqual, GetDisplayName(propName), expectedValue), [propName]));
            }
        }

        private void MustContain(IEnumerable<string> propValue, string propName, string expectedValue)
        {
            if (!propValue.Contains(expectedValue, StringComparer.OrdinalIgnoreCase))
            {
                _validationResults.Add(new System.ComponentModel.DataAnnotations.ValidationResult(string.Format(Constants.ValidationErrorMessages.MustContain, GetDisplayName(propName), expectedValue), [propName]));
            }
        }

        private async Task<ValidationResult> ValidateRequestSignature(ClientRegistrationRequest request, ConfigurationOptions configOptions)
        {
            // Check the Data Recipient's JWKS from the Software Statement.
            if (string.IsNullOrEmpty(request.SoftwareStatement.JwksUri) || !Uri.IsWellFormedUriString(request.SoftwareStatement.JwksUri, UriKind.Absolute))
            {
                _logger.LogError("Invalid jwks_uri in SSA");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_JWKS_URI);
            }

            // Get the Data Recipient's JWKS from the Software Statement.
            var jwks = await _jwksService.GetJwks(new Uri(request.SoftwareStatement.JwksUri), request.Kid);
            if (jwks == null || jwks.Keys.Count == 0)
            {
                _logger.LogError("Could not load JWKS from Data Recipient endpoint: {JwksUri}", request.SoftwareStatement.JwksUri);
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.UNABLE_TO_LOAD_JWKS_DATA_RECIPIENT, request.SoftwareStatement.JwksUri);
            }

            _logger.LogInformation("Data Recipient JWKS: {Jwks}", JsonConvert.SerializeObject(jwks));

            // Assert - Validate Registration Request Signature
            var validationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(configOptions.ClockSkewSeconds),

                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jwks.Keys,

                ValidateAudience = true,
                ValidAudience = configOptions.Issuer,

                ValidateIssuer = true,
                ValidIssuer = request.SoftwareStatement.SoftwareId,
            };

            // Validate token.
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(request.ClientRegistrationRequestJwt, validationParameters, out var _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client Registration Request validation failed - {Message}", ex.Message);
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REGISTRATION_REQUEST_VALIDATION_FAILED);
            }

            return ValidationResult.Pass();
        }

        /// <summary>
        /// Validate the sectory identifier url.
        /// Currently it is only required to call this endpoint and we do not validate the output.
        /// </summary>
        private async Task<ValidationResult> ValidateSectorIdentifierUri(string? sectorIdentifierUri)
        {
            if (string.IsNullOrEmpty(sectorIdentifierUri))
            {
                _logger.LogDebug("Sector Identifier URI not found");
                return ValidationResult.Pass();
            }

            _logger.LogDebug("Sending a request to: {SectorIdentifierUri}", sectorIdentifierUri);

            var sectorIdClient = new HttpClient(HttpHelper.CreateHttpClientHandler(_configuration));
            var sectorIdResponse = await sectorIdClient.GetAsync(sectorIdentifierUri);

            if (sectorIdResponse != null && sectorIdResponse.IsSuccessStatusCode)
            {
                // need to validate the contents of the sector identifier.
                return ValidationResult.Pass();
            }

            _logger.LogError("Invalid sector_identifier_uri");
            return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_SECTOR_IDENTIFIER_URI);
        }

        private async Task<ValidationResult> ValidateSSA(ClientRegistrationRequest clientRegistrationRequest, ConfigurationOptions configOptions)
        {
            if (clientRegistrationRequest.SoftwareStatement == null)
            {
                _logger.LogError("The software_statement is empty or invalid");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.SOFTWARE_STATEMENT_INVALID_OR_EMPTY);
            }

            // Get the SSA JWKS from the Register.
            var ssaJwks = await GetSsaJwks(clientRegistrationRequest);
            if (ssaJwks == null || ssaJwks.Keys.Count == 0)
            {
                _logger.LogError("Could not load SSA JWKS from Register endpoint: {SsaJwksUri}", configOptions.CdrRegister!.SsaJwksUri);
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.UNABLE_TO_LOAD_JWKS_FROM_REGISTER, configOptions.CdrRegister.SsaJwksUri);
            }

            _logger.LogInformation("Register SSA JWKS: {SsaJwks}", JsonConvert.SerializeObject(ssaJwks));

            // Assert - Validate SSA Signature
            var validationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(configOptions.ClockSkewSeconds),

                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = ssaJwks.Keys,

                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = "cdr-register",
            };

            // Validate token.
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(clientRegistrationRequest.SoftwareStatementJwt, validationParameters, out var _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSA validation failed.");
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.SSA_VALIDATION_FAILED);
            }

            return ValidationResult.Pass();
        }

        private async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> GetSsaJwks(ClientRegistrationRequest clientRegistrationRequest)
        {
            // Remove this logic once CTS has removed the use of the x-cts-ssa-publickey header for passing SSA JWKS public key.
            // Check if the SSA JWKS public key was passed as a http header (CTS).
            var ssaJwksHeaderName = _configuration.GetValue<string>("CdrAuthServer:RegisterSsaPublicKeyHttpHeaderName", "");
            if (!string.IsNullOrEmpty(ssaJwksHeaderName)
                && _httpContextAccessor != null
                && _httpContextAccessor.HttpContext != null
                && _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(ssaJwksHeaderName, out StringValues publicKey)
                && !string.IsNullOrEmpty(publicKey))
            {
               
                _logger.LogInformation("SSA JWKS found in http header {Header}: {Value}", ssaJwksHeaderName, publicKey);

                var jwk = new Microsoft.IdentityModel.Tokens.JsonWebKey(publicKey);
                if (jwk.Kid == clientRegistrationRequest.SoftwareStatement.Header.Kid)
                {
                    var jwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet();
                    jwks.Keys.Add(jwk);
                    return jwks;
                }
            }

            // Retrieve the JWKS from the Register's SSA JWKS endpoint.
            var configOptions = _configuration.GetConfigurationOptions();
            _logger.LogInformation("Retrieving SSA JWKS from Register {Uri}...", configOptions?.CdrRegister?.SsaJwksUri);
            return await _jwksService.GetJwks(new Uri(configOptions?.CdrRegister?.SsaJwksUri ?? "about:blank"), clientRegistrationRequest.SoftwareStatement.Header.Kid);
        }

        private static string GetDisplayName(string propName)
        {
            var propInfo = typeof(ClientRegistrationRequest).GetProperty(propName);

            if (propInfo == null)
            {
                return propName;
            }

            var displayAttr = propInfo.GetCustomAttributes(false).OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();

            if (displayAttr != null)
            {
                return displayAttr.Name ?? String.Empty;
            }

            return propName;
        }
    }
}
