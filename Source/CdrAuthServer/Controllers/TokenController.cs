using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        [ValidateMtls]
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
            var validationResult = await _tokenRequestValidator.Validate(User.GetClientId(), tokenRequest, configOptions);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed - {@validationResult}", validationResult);
                return new JsonResult(new Error(validationResult.Error, validationResult.ErrorDescription)) { StatusCode = validationResult.StatusCode ?? 400 };
            }

            var cnf = GetClientCertificateThumbprint();
            var tokenResponse = await _tokenService.IssueTokens(tokenRequest, cnf, configOptions);

            if (tokenResponse.Error != null)
            {
                _logger.LogError("IssueTokens failed - {@error}", tokenResponse.Error);
                return BadRequest(tokenResponse.Error);
            }

            return new JsonResult(tokenResponse, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private string GetClientCertificateThumbprint()
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            if (this.HttpContext.Request.Headers.TryGetValue(configOptions.ClientCertificateThumbprintHttpHeaderName, out StringValues headerThumbprints))
            {
                return headerThumbprints.First();
            }

            return "";
        }
    }
}
