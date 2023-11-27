using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class ArrangementRevocationController : ControllerBase
    {
        private readonly ILogger<ArrangementRevocationController> _logger;
        private readonly IConfiguration _config;
        private readonly IClientService _clientService;
        private readonly IGrantService _grantService;
        private readonly ICdrService _cdrService;

        public ArrangementRevocationController(
            ILogger<ArrangementRevocationController> logger,
            IConfiguration config,
            IClientService clientService,
            IGrantService grantService,
            ICdrService cdrService)
        {
            _logger = logger;
            _config = config;
            _clientService = clientService;
            _grantService = grantService;
            _cdrService = cdrService;
        }

        [HttpPost]
        [Route("/connect/arrangements/revoke")]
        [ServiceFilter(typeof(ValidateMtlsAttribute))]
        [ValidateClientAssertion]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RevokeArrangement(
            [FromForm(Name = ClaimNames.CdrArrangementId)] string cdrArrangementId)
        {
            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                _logger.LogError("Missing required field of cdrArrangementId");
                return BadRequest(CdsErrorList.MissingRequiredField(ClaimNames.CdrArrangementId));
            }

            var client = await _clientService.Get(User.GetClientId());
            if (client == null)
            {
                _logger.LogError("Client not found {@User}", User);
                return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.CLIENT_NOT_FOUND);
            }

            // Check software product status (if configured).
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            if (configOptions?.CdrRegister?.CheckSoftwareProductStatus is true)
            {
                var softwareProductId = client.SoftwareId;
                var softwareProduct = await _cdrService.GetSoftwareProduct(softwareProductId);
                if (softwareProduct == null)
                {
                    _logger.LogError("Software Product not found {softwareProductId}", softwareProductId);
                    return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.SOFTWARE_PRODUCT_NOT_FOUND);
                }
                if (softwareProduct.Status.Equals("REMOVED", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Software product status is removed - consents cannot be revoked {softwareProductId}", softwareProductId);
                    return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.SOFTWARE_PRODUCT_REMOVED);
                }
            }

            if (await _grantService.Get(GrantTypes.CdrArrangement, cdrArrangementId, client.ClientId) is not CdrArrangementGrant cdrArrangementGrant)
            {
                _logger.LogError("{arrangement} with id:{id} not found for client:{clientid}", GrantTypes.CdrArrangement, cdrArrangementId, client.ClientId);               
                return UnprocessableEntity(CdsErrorList.InvalidConsentArrangement(cdrArrangementId));
            }

            // Delete the grants.
            await _grantService.Delete(client.ClientId, GrantTypes.RefreshToken, cdrArrangementGrant.RefreshToken);
            await _grantService.Delete(client.ClientId, GrantTypes.CdrArrangement, cdrArrangementGrant.Key);

            return NoContent();
        }

    }
}
