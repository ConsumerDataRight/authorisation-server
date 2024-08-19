using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class IntrospectionController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<IntrospectionController> _logger;
        private readonly IGrantService _grantService;
        private readonly ITokenService _tokenService;
        private readonly IClientService _clientService;
        private readonly ICdrService _cdrService;

        public IntrospectionController(
            IConfiguration config,
            IGrantService grantService,
            ITokenService tokenService,
            ICdrService cdrService,
            IClientService clientService,
            ILogger<IntrospectionController> logger)
        {
            _config = config;
            _logger = logger;
            _tokenService = tokenService;
            _grantService = grantService;
            _cdrService = cdrService;
            _clientService = clientService;
        }

        [HttpPost]
        [Route("/connect/introspect")]
        [ApiVersionNeutral]
        [ServiceFilter(typeof(ValidateMtlsAttribute))]
        [ValidateClientAssertion]
        public async Task<JsonResult> Introspect([FromForm] string token)
        {
            // Check that refresh token is present
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("token is empty or null - {Token}", token);
                return new JsonResult(new Introspection
                {
                    IsActive = false,
                });
            }

            // Check if the refresh token has been revoked.
            if (await _tokenService.IsTokenBlacklisted(token))
            {
                _logger.LogError("token is blacklisted - {Token}", token);
                return new JsonResult(new Introspection
                {
                    IsActive = false,
                });
            }

            // Get the refresh token
            if ((await _grantService.Get(GrantTypes.RefreshToken, token, User.Identity?.Name)) is RefreshTokenGrant grant && !grant.IsExpired)
            {
                return new JsonResult(new Introspection
                {
                    IsActive = true,
                    CdrArrangementId = grant.CdrArrangementId ?? string.Empty,
                    Expiry = grant.ExpiresAt.ToEpoch(),
                    Scope = grant.Scope
                });
            }

            _logger.LogError("get refesh token failed - {Token}", token);
            return new JsonResult(new Introspection
            {
                IsActive = false,
            });
        }

        /// <summary>
        /// This controller action is used to check the validity of an access_token only.
        /// It should not be called by an external participant (i.e. ADR) but is consumed internally
        /// by the resource API of the mock data holder.
        /// In the CDS, the introspection endpoint only supports the introspection of refresh tokens.
        /// </summary>
        /// <param name="token">Access token to check</param>
        /// <returns>IntrospectionResult</returns>
        /// <remarks>
        /// There is currently no auth on this endpoint.  
        /// This could be added in the future to only allow the calls from the Mock Data Holder Resource API.
        /// </remarks>
        [HttpPost]
        [Route("connect/introspect-internal")]
        [ApiVersionNeutral]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> IntrospectInternal([Required, FromForm] string token)
        {
            // Check if the token has been revoked - this will work for a revoked refresh token.
            if (string.IsNullOrEmpty(token) || await _tokenService.IsTokenBlacklisted(token))
            {
                _logger.LogError("token is nullorempty or blacklisted - {Token}", token);
                return new JsonResult(new Introspection
                {
                    IsActive = false,
                });
            }

            // Check if the token is an access token.
            var tokenHandler = new JwtSecurityTokenHandler();
            var isAccessToken = tokenHandler.CanReadToken(token);

            // Check if the access token has been revoked.
            if (isAccessToken)
            {
                var securityToken = tokenHandler.ReadJwtToken(token);
                var jti = securityToken.Claims.GetClaimValue(ClaimNames.JwtId);
                if (!string.IsNullOrEmpty(jti) && await _tokenService.IsTokenBlacklisted(jti))
                {
                    _logger.LogError("access token jti is blacklisted - {Jti}", jti);
                    return new JsonResult(new Introspection
                    {
                        IsActive = false,
                    });
                }

                var clientIdFromAccessToken = securityToken.Claims.GetClaimValue(ClaimNames.ClientId);
                var authCode = securityToken.Claims.GetClaimValue(ClaimNames.AuthorizationCode);
                if (!string.IsNullOrEmpty(authCode) && await _tokenService.IsTokenBlacklisted($"{clientIdFromAccessToken}::{authCode}"))
                {
                    _logger.LogError("access token auth code is blacklisted - {AuthCode}", authCode);
                    return new JsonResult(new Introspection
                    {
                        IsActive = false,
                    });
                }

                // Check software product status (if configured).
                var configOptions = _config.GetConfigurationOptions(this.HttpContext);
                if (configOptions.CdrRegister.CheckSoftwareProductStatus)
                {
                    var client = await _clientService.Get(clientIdFromAccessToken);
                    if (client == null)
                    {
                        return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.CLIENT_NOT_FOUND);
                    }

                    var softwareProduct = await _cdrService.GetSoftwareProduct(client.SoftwareId);
                    if (softwareProduct == null)
                    {
                        return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.SOFTWARE_PRODUCT_NOT_FOUND);
                    }
                    if (!softwareProduct.IsActive())
                    {
                        return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.SOFTWARE_PRODUCT_STATUS_INACTIVE, softwareProduct.GetStatusDescription());
                    }
                }

                // Perform further checking.
                var cdrArrangementId = securityToken.Claims.GetClaimValue(ClaimNames.CdrArrangementId);
                var cdrArrangementVersion = securityToken.Claims.GetClaimValue(ClaimNames.CdrArrangementVersion);

                // Access token is related to a cdr arrangement.
                if (!string.IsNullOrEmpty(cdrArrangementId))
                {
                    var arrangement = await _grantService.Get(GrantTypes.CdrArrangement, cdrArrangementId, clientIdFromAccessToken) as CdrArrangementGrant;
                    if (arrangement == null)
                    {
                        _logger.LogError("arrangement was not found: {CdrArrangementId}", cdrArrangementId);
                        return new JsonResult(new Introspection
                        {
                            IsActive = false
                        });
                    }

                    if (!string.IsNullOrEmpty(cdrArrangementVersion) && !arrangement.Version.ToString().Equals(cdrArrangementVersion))
                    {
                        _logger.LogError("arrangement version ({Version}) does not match arrangement version in the access token: {CdrArrangementVersion}", arrangement.Version, cdrArrangementVersion);
                        return new JsonResult(new Introspection
                        {
                            IsActive = false
                        });
                    }

                    // If the arrangement was not found, or has expired, or does not match the client id in the access token.
                    return new JsonResult(new Introspection
                    {
                        IsActive = (arrangement != null && !arrangement.IsExpired && !securityToken.IsExpired())
                    });
                }
            }

            return new JsonResult(new Introspection
            {
                IsActive = true
            });
        }
    }
}
