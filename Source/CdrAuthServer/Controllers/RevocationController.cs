using CdrAuthServer.Extensions;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class RevocationController : ControllerBase
    {
        private readonly ILogger<RevocationController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IGrantService _grantService;

        public RevocationController(
            ILogger<RevocationController> logger,
            ITokenService tokenService,
            IGrantService grantService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _grantService = grantService;
        }

        [HttpPost]
        [Route("connect/revocation")]
        [ApiVersionNeutral]
        [ServiceFilter(typeof(ValidateMtlsAttribute))]
        [ValidateClientAssertion]
        public async Task<ActionResult> RevokeToken(
            [FromForm] string token,
            [FromForm(Name = "token_type_hint")] string tokenTypeHint)
        {
            var clientId = this.User.GetClientId();

            // Attempt to find a matching refresh token.
            var refreshTokenGrant = await _grantService.Get(GrantTypes.RefreshToken, token, clientId);
            if (refreshTokenGrant != null)
            {
                _logger.LogInformation("Revoked the refresh token by removing the refresh token grant for clientId:{Id}", clientId);
                // Revoke the refresh token by removing the refresh token grant.
                await _grantService.Delete(clientId, GrantTypes.RefreshToken, token);
                return Ok();
            }

            // The token wasn't a refresh token, so now blacklist the access token.
            try
            {
                // Only revoke the access token if the current client owns the access token.
                var securityTokenHandler = new JwtSecurityTokenHandler();

                // Not a valid JWT access token, so exit.
                if (!securityTokenHandler.CanReadToken(token))
                {
                    return Ok();
                }

                var securityToken = securityTokenHandler.ReadJwtToken(token);
                if (securityToken != null)
                {
                    var clientIdFromAccessToken = securityToken.Claims.GetClaimValue(ClaimNames.ClientId);

                    _logger.LogDebug("Incoming client id: {ClientId}", clientId);
                    _logger.LogDebug("Access token client id: {ClientId}", clientIdFromAccessToken);

                    if (clientId != null && clientId.Equals(clientIdFromAccessToken, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Revoking access token: {token}", token);
                        await _tokenService.AddToBlacklist(securityToken.Claims.GetClaimValue(ClaimNames.JwtId));

                        return Ok();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while revoking the access token: {token}", token);
            }

            // Always return 200 OK.
            return Ok();
        }

    }
}
