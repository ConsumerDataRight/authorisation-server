using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ILogger<TokenController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly ITokenRequestValidator _tokenRequestValidator;

        public TokenController(
            IConfiguration config,
            ILogger<TokenController> logger,
            ITokenService tokenService,
            ITokenRequestValidator tokenRequestValidator)
        {
            _config = config;
            _logger = logger;
            _tokenService = tokenService;
            _tokenRequestValidator = tokenRequestValidator;
        }

        [HttpPost]
        [Route("connect/token")]
        [ApiVersionNeutral]
        [ServiceFilter(typeof(ValidateMtlsAttribute))]
        [ValidateClientAssertion(true)]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> IssueTokens([FromForm] TokenRequest tokenRequest)
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var clientId = User.GetClientId();
            var validationResult = await _tokenRequestValidator.Validate(clientId, tokenRequest, configOptions);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed - {@ValidationResult}", validationResult);
                return new JsonResult(new Error(validationResult.Error ?? string.Empty, validationResult.ErrorDescription)) { StatusCode = validationResult.StatusCode ?? 400 };
            }

            // Client Id is optional in the token request from the client but is required in the token repsonse.
            // If client Id is not provided in the request then use the client Id that was extracted from the client assertion.
            if (string.IsNullOrEmpty(tokenRequest.Client_id) && !string.IsNullOrEmpty(clientId))
            {
                tokenRequest.Client_id = clientId;
            }

            var cnf = GetClientCertificateThumbprint();
            var tokenResponse = await _tokenService.IssueTokens(tokenRequest, cnf, configOptions);

            if (tokenResponse.Error != null)
            {
                _logger.LogError("IssueTokens failed - {@Error}", tokenResponse.Error);
                return BadRequest(tokenResponse.Error);
            }

            return new JsonResult(tokenResponse, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private string GetClientCertificateThumbprint()
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            if (this.HttpContext.Request.Headers.TryGetValue(configOptions.ClientCertificateThumbprintHttpHeaderName, out StringValues headerThumbprints))
            {
                return headerThumbprints[0] ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
