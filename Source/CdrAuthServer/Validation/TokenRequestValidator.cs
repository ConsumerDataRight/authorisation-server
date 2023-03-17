using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    public class TokenRequestValidator : ITokenRequestValidator
    {
        private readonly IGrantService _grantService;
        private readonly ITokenService _tokenService;
        private readonly IClientService _clientService;
        private readonly ICdrService _cdrService;

        public TokenRequestValidator(
            IGrantService grantService,
            ITokenService tokenService,
            IClientService clientService,
            ICdrService cdrService)
        {
            _grantService = grantService;
            _tokenService = tokenService;
            _clientService = clientService;
            _cdrService = cdrService;
        }

        public async Task<ValidationResult> Validate(string? clientId, TokenRequest tokenRequest, ConfigurationOptions configOptions)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ID_MISSING);
            }

            if (tokenRequest == null)
            {
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_TOKEN_REQUEST);
            }

            if (string.IsNullOrEmpty(tokenRequest.grant_type))
            {
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.GRANT_TYPE_MISSING);
            }

            if (configOptions.GrantTypesSupported?.Contains(tokenRequest.grant_type) is not true)
            {
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.UNSUPPORTED_GRANT_TYPE);
            }

            // If the client_id was passed, then it should match the client making the request.
            if (!string.IsNullOrEmpty(tokenRequest.client_id) && !clientId.Equals(tokenRequest.client_id, StringComparison.OrdinalIgnoreCase))
            {
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CLIENT_ID_MISMATCH);
            }

            // Check the software product id status.
            var client = await _clientService.Get(clientId);
            if (client == null || string.IsNullOrEmpty(client.SoftwareId))
            {
                return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.UNABLE_TO_RETRIEVE_CLIENT_META_DATA);
            }

            // Check software product status (if configured).
            if (configOptions.CdrRegister.CheckSoftwareProductStatus)
            {
                var softwareProduct = await _cdrService.GetSoftwareProduct(client.SoftwareId);
                if (softwareProduct == null)
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.SOFTWARE_PRODUCT_NOT_FOUND);
                }
                if (!softwareProduct.IsActive())
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.SOFTWARE_PRODUCT_STATUS_INACTIVE, softwareProduct.GetStatusDescription());
                }
            }

            // Auth code request validation.
            if (tokenRequest.grant_type == GrantTypes.AuthCode)
            {
                if (string.IsNullOrEmpty(tokenRequest.code))
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CODE_IS_MISSING);

                }

                // Redirect URI validation.
                if (string.IsNullOrEmpty(tokenRequest.redirect_uri))
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REDIRECT_URI_IS_MISSING);
                }

                // Code verifier validation.
                if (string.IsNullOrEmpty(tokenRequest.code_verifier))
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.CODE_VERIFIER_IS_MISSING);
                }

                // Find the matching auth code grant.
                var authCodeGrant = await _grantService.Get(GrantTypes.AuthCode, tokenRequest.code, clientId) as AuthorizationCodeGrant;
                if (authCodeGrant == null)
                {
                    // Blacklist the auth code as it may have been a re-use attempt.
                    await _tokenService.AddToBlacklist($"{clientId}::{tokenRequest.code}");
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_AUTHORIZATION_CODE);
                }

                if (authCodeGrant.IsExpired)
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.AUTHORIZATION_CODE_EXPIRED);
                }

                // Get the original auth request object.
                var authRequestObject = JsonConvert.DeserializeObject<AuthorizationRequestObject>(authCodeGrant.Request);

                // Verify the redirect_uri.
                if (authRequestObject?.RedirectUri != tokenRequest.redirect_uri)
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REDIRECT_URI_AUTHORIZATION_REQUEST_MISMATCH);
                }

                // Verify the code_verifier.
                var expectedCodeChallenge = authRequestObject.CodeChallenge;
                var codeChallenge = CreatePkceChallenge(tokenRequest.code_verifier);

                if (expectedCodeChallenge != codeChallenge)
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_CODE_VERIFIER);
                }
            }

            // Refresh token request validation.
            if (tokenRequest.grant_type == GrantTypes.RefreshToken)
            {
                if (string.IsNullOrEmpty(tokenRequest.refresh_token))
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REFRESH_TOKEN_MISSING);
                }

                // Find the matching refresh token grant.
                var refreshTokenGrant = await _grantService.Get(GrantTypes.RefreshToken, tokenRequest.refresh_token, clientId);
                if (refreshTokenGrant == null)
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.INVALID_REFRESH_TOKEN);
                }

                if (refreshTokenGrant.IsExpired)
                {
                    return ErrorCatalogue.Catalogue().GetValidationResult(ErrorCatalogue.REFRESH_TOKEN_EXPIRED);
                }
            }

            return ValidationResult.Pass();
        }

        private static string CreatePkceChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Base64UrlEncoder.Encode(challengeBytes);
            }
        }
    }
}
