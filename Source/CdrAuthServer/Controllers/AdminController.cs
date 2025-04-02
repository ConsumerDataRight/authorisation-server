using CdrAuthServer.Authorisation;
using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Attributes;
using CdrAuthServer.Infrastructure.Authorisation;
using CdrAuthServer.Models;
using CdrAuthServer.Models.Json;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ICdrService _cdrService;
        private readonly IClientService _clientService;
        private readonly ILogger<AdminController> _logger;
        private readonly IRegisterClientService _registerClientService;

        public AdminController(
            ICdrService cdrService,
            IClientService clientService,
            ILogger<AdminController> logger,
            IRegisterClientService registerClientService)
        {
            _cdrService = cdrService;
            _clientService = clientService;
            _logger = logger;
            _registerClientService = registerClientService;
        }

        [HttpPost]
        [Route("cds-au/v1/admin/register/metadata")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthServerAuthorisationPolicyAttribute.AdminMetadataUpdate)]
        [ApiVersion("1")]
        [ReturnXV("1")]
        public async Task<IActionResult> RefreshDataRecipients(DataRecipientRequest dataRecipientRequest, CancellationToken cancellationToken = default)
        {
            if (dataRecipientRequest is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Invalid Data recipient");
            }

            // Authorization server backend validations for client id
            var clientIdResult = await ValidateClientId();

            if (!clientIdResult.IsValid)
            {
                _logger.LogInformation("Validation failed - {@ClientIdResult}", clientIdResult);
                Response.Headers.Append(
                    HttpHeaders.WWWAuthenticate,
                    $"Bearer error=\"{clientIdResult.Error}\", error_description=\"{clientIdResult.ErrorDescription}\"");
                return new UnauthorizedObjectResult(new { error = clientIdResult.Error, error_description = clientIdResult.ErrorDescription });
            }

            if (string.Equals(dataRecipientRequest.Data?.Action, "REFRESH", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _registerClientService.GetDataRecipients(cancellationToken);

                if (response != null)
                {
                    List<SoftwareProduct> softwareProducts = MapSoftwareProductList(response.Data.ToList());

                    // Purge Data Recipients
                    await _cdrService.PurgeDataRecipients();

                    // If data was retrieved, then insert it in our repository.
                    if (softwareProducts.Count > 0)
                    {
                        await _cdrService.InsertDataRecipients(softwareProducts);
                        return Ok($"Data recipient records refreshed from {response.Links.Self}.");
                    }
                }
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Data recipient data could not be refreshed.");
        }

        private static List<SoftwareProduct> MapSoftwareProductList(List<LegalEntity> dataRecipients)
        {
            return dataRecipients
                .SelectMany(legalEntity => legalEntity.DataRecipientBrands
                .SelectMany(dataRecipientBrand => dataRecipientBrand.SoftwareProducts
                .Select(sp => new SoftwareProduct
                {
                    SoftwareProductId = sp.SoftwareProductId,
                    SoftwareProductName = sp.SoftwareProductName,
                    SoftwareProductDescription = sp.SoftwareProductDescription,
                    LogoUri = sp.LogoUri,
                    Status = sp.Status,

                    LegalEntityId = legalEntity.LegalEntityId,
                    LegalEntityName = legalEntity.LegalEntityName,
                    LegalEntityStatus = legalEntity.Status,

                    BrandId = dataRecipientBrand.DataRecipientBrandId,
                    BrandName = dataRecipientBrand.BrandName,
                    BrandStatus = dataRecipientBrand.Status,
                })))
                .ToList();
        }

        private async Task<ValidationResult> ValidateClientId()
        {
            var clientIdClaimValue = this.User.Claims.FirstOrDefault(c => c.Type == ClaimNames.ClientId);
            if (clientIdClaimValue == null
                || (await _clientService.Get(clientIdClaimValue.Value) == null))
            {
                return ValidationResult.Fail(ErrorCodes.Generic.InvalidRequest, "The client is unknown");
            }

            return ValidationResult.Pass();
        }
    }
}
