using CdrAuthServer.Authorisation;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure.Attributes;
using CdrAuthServer.Infrastructure.Authorisation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CdrAuthServer.Controllers
{
    /// <summary>
    /// This controller is used to provide a resource endpoint for testing purposes, to
    /// ensure that the auth server is issuing access tokens correctly.
    /// </summary>
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private readonly ILogger<ResourceController> _logger;
        private readonly IConfiguration _config;

        public ResourceController(
            ILogger<ResourceController> logger,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("resource/cds-au/v1/common/customer")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthServerAuthorisationPolicyAttribute.GetCustomerBasic)]
        [ApiVersion("1")]
        [ReturnXV("1")]
        public IActionResult GetCustomer()
        {
            _logger.LogInformation("Request received to /resource/cds-au/v1/common/customer");

            if (_config.GetValue<bool>("CdrAuthServer:ValidateResourceEndpoint", true))
            {
                // Add validation for the resource endpoint.
                var (isValidAuthDate, authDateError, authDateStatusCode) = HttpContext.Request.Headers.ValidateAuthDate();
                if (!isValidAuthDate)
                {
                    _logger.LogError("ValidateAuthDate failed - {@AuthDateError}", authDateError);
                    return new JsonResult(authDateError) { StatusCode = authDateStatusCode ?? 400 };
                }
            }

            var body =
                "{ " +
                " \"data\": { " +
                "   \"customerUType\": \"person\", " +
                "   \"person\": { " +
                "     \"lastUpdateTime\": \"2021-03-01T18:30:00Z\", " +
                "     \"firstName\": \"Kamilla\", " +
                "     \"lastName\": \"Smith\", " +
                "     \"middleNames\": [ " +
                "       \"O\" " +
                "     ], " +
                "     \"prefix\": \"Mrs\", " +
                "     \"suffix\": \"\", " +
                "     \"occupationCode\": \"123412\", " +
                "     \"occupationCodeVersion\": \"ANZSCO_1220.0_2013_V1.3\" " +
                "    } " +
                "  }, " +
                "  \"links\": { " +
                "    \"self\": \"https://localhost:8081/resource/cds-au/v1/common/customer\" " +
                "  }, " +
                "  \"meta\": { } " +
                "}";

            var xFapiInterationId = Guid.NewGuid().ToString();
            if (Request.Headers.ContainsKey("x-fapi-interaction-id"))
            {
                xFapiInterationId = Request.Headers["x-fapi-interaction-id"].ToString();
            }

            Response.Headers.Append("x-fapi-interaction-id", xFapiInterationId);
            return Content(body, "application/json");
        }

        // TODO: Remove this once CTS has removed the call to DH Get Accounts endpoint.
        // This API operation method has been put in place in order to pass CTS testing only.
        [HttpGet]
        [Route("resource/cds-au/v1/{industry}/accounts")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthServerAuthorisationPolicyAttribute.GetBankingAccounts)]
        [ApiVersion("1")]
        [ReturnXV("1")]
        public IActionResult GetAccounts(string industry)
        {
            _logger.LogInformation("Request received to /resource/cds-au/v1/{Industry}/accounts", industry);

            if (_config.GetValue<bool>("CdrAuthServer:ValidateResourceEndpoint", true))
            {
                // Add validation for the resource endpoint.
                var (isValidAuthDate, authDateError, authDateStatusCode) = HttpContext.Request.Headers.ValidateAuthDate();
                if (!isValidAuthDate)
                {
                    _logger.LogError("ValidateAuthDate failed - {@AuthDateError}", authDateError);
                    return new JsonResult(authDateError) { StatusCode = authDateStatusCode ?? 400 };
                }
            }

            var body =
                "{ " +
                " \"data\": { " +
                "   \"accounts\": [] " +
                "  }, " +
                "  \"links\": { " +
                "    \"self\": \"https://localhost:8081/resource/cds-au/v1/" + industry + "/accounts\" " +
                "  }, " +
                "  \"meta\": { } " +
                "}";

            var xFapiInterationId = Guid.NewGuid().ToString();
            if (Request.Headers.ContainsKey("x-fapi-interaction-id"))
            {
                xFapiInterationId = Request.Headers["x-fapi-interaction-id"].ToString();
            }

            Response.Headers.Append("x-fapi-interaction-id", xFapiInterationId);
            return Content(body, "application/json");
        }
    }
}
