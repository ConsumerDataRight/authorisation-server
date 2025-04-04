﻿using CdrAuthServer.Domain;
using CdrAuthServer.Domain.Models;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class PushedAuthorisationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<PushedAuthorisationController> _logger;
        private readonly IGrantService _grantService;
        private readonly IParValidator _parValidator;

        public PushedAuthorisationController(
            IConfiguration config,
            IGrantService grantService,
            ILogger<PushedAuthorisationController> logger,
            IParValidator parValidator)
        {
            _config = config;
            _grantService = grantService;
            _logger = logger;
            _parValidator = parValidator;
        }

        [HttpPost]
        [Route("/connect/par")]
        [ApiVersionNeutral]
        [ServiceFilter(typeof(ValidateMtlsAttribute))]
        [ValidateClientAssertion]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PushedAuthorizationCreatedResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PushedAuthorisationRequest(
            [FromForm(Name = "request")] string request)
        {
            var options = _config.GetConfigurationOptions(this.HttpContext);

            // Validate the request object jwt.
            if (string.IsNullOrEmpty(request))
            {
                _logger.LogError("request is null or empty");
                return new BadRequestObjectResult(new ResponseErrorList().AddMissingRequiredField("request"));
            }

            if (!string.IsNullOrEmpty(Request.GetFormFieldValue("request_uri")))
            {
                _logger.LogError("request_uri form parameter is null or empty");
                return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.PAR_REQUEST_URI_FORM_PARAMETER_NOT_SUPPORTED);
            }

            var clientId = User.GetClientId();
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var (result, validatedRequest) = await _parValidator.Validate(clientId ?? string.Empty, request, configOptions);
            if (!result.IsValid)
            {
                _logger.LogError("parvalidator returned error:{Error} errordescription:{Desc}", result.Error, result.ErrorDescription);
                return new JsonResult(new Error(result.Error ?? string.Empty, result.ErrorDescription)) { StatusCode = result.StatusCode };
            }

            // Create the par grant.
            var parGrant = new RequestUriGrant()
            {
                GrantType = GrantTypes.RequestUri,
                Key = $"urn:{Guid.NewGuid()}",
                ClientId = clientId ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(options.RequestUriExpirySeconds),
                Request = JsonConvert.SerializeObject(validatedRequest),
            };

            await _grantService.Create(parGrant);

            var parCreatedResponse = new PushedAuthorizationCreatedResponse()
            {
                RequestUri = parGrant.Key,
                ExpiresIn = options.RequestUriExpirySeconds,
            };

            return new JsonResult(parCreatedResponse) { StatusCode = 201 };
        }
    }
}
