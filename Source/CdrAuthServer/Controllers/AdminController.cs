using CdrAuthServer.Authorisation;
using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure;
using CdrAuthServer.Models;
using CdrAuthServer.Models.Json;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ConfigurationOptions _configOptions;
        private readonly ICdrService _cdrService;
        private readonly IClientService _clientService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IConfiguration config,
            ICdrService cdrService,
            IClientService clientService,
            ILogger<AdminController> logger)
        {
            _cdrService = cdrService;
            _clientService = clientService;
            _logger = logger;
            _configOptions = config.GetConfigurationOptions();
        }

        [HttpPost]        
        [Route("cds-au/v1/admin/register/metadata")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthorisationPolicy.AdminMetadataUpdate)]
        public async Task<IActionResult> RefreshDataRecipients(DataRecipientRequest dataRecipientRequest)
        {
            Response.Headers.Add("x-v", "1");

            if (dataRecipientRequest is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Invalid Data recipient");
            }

            // Authorization server backend validations for client id
            var clientIdResult = await ValidateClientId();

            if (!clientIdResult.IsValid)
            {
                _logger.LogInformation("Validation failed - {@clientIdResult}", clientIdResult);
                Response.Headers.Append(
                    HttpHeaders.WWWAuthenticate,
                    $"Bearer error=\"{clientIdResult.Error}\", error_description=\"{clientIdResult.ErrorDescription}\"");
                return new UnauthorizedObjectResult(new { error = clientIdResult.Error, error_description = clientIdResult.ErrorDescription });
            }

            if (dataRecipientRequest != null && string.Equals( dataRecipientRequest.Data?.Action, "REFRESH",StringComparison.OrdinalIgnoreCase))
            {
                var getDataRecipientsEndpoint = _configOptions.CdrRegister.GetDataRecipientsEndpoint;                
                if (!string.IsNullOrEmpty(getDataRecipientsEndpoint))
                {
                    // Validate Request headers for x-v and x-min-v versions
                    var (isValidVersion, versionError, versionErrorStatusCode) = HttpContext.Request.Headers.ValidateVersion();
                    if (!isValidVersion)
                    {
                        _logger.LogError("Version validation failed - {@versionError}", versionError);
                        return new JsonResult(versionError) { StatusCode = versionErrorStatusCode ?? 400 };
                    }

                    // Call the Register to get the data recipients list.
                    // With new endpoints x-v header version should be 3
                    var jsonResponse = await GetData(getDataRecipientsEndpoint, _configOptions.CdrRegister.Version);

                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        var data = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                        if (data != null)
                        {
                            var dataRecipients = data["data"].ToObject<LegalEntity[]>();

                            List<SoftwareProduct> softwareProducts = await MapSoftwareProductList(dataRecipients?.ToList());

                            // Purge Data Recipients
                            await _cdrService.PurgeDataRecipients();

                            // If data was retrieved, then insert it in our repository.
                            if (softwareProducts.Any())
                            {
                                await _cdrService.InsertDataRecipients(softwareProducts);
                                return Ok($"Data recipient records refreshed from {getDataRecipientsEndpoint}.");
                            }
                        }
                    }
                }
            }            
            return StatusCode(StatusCodes.Status500InternalServerError, "Data recipient data could not be refreshed.");
        }

        private async Task<List<SoftwareProduct>> MapSoftwareProductList(List<LegalEntity?> dataRecipients)
        {
            List<SoftwareProduct> softwareProducts = new List<SoftwareProduct>();

            if (dataRecipients.Any())
            {
                foreach (LegalEntity legalEntity in dataRecipients) 
                {                                         
                    foreach (var dataRecipientBrand in legalEntity.DataRecipientBrands)
                    {                        
                        foreach (var sp in dataRecipientBrand.SoftwareProducts)
                        {
                            SoftwareProduct softwareProduct = new SoftwareProduct();
                            softwareProduct.SoftwareProductId = sp.SoftwareProductId;
                            softwareProduct.SoftwareProductName = sp.SoftwareProductName;
                            softwareProduct.SoftwareProductDescription = sp.SoftwareProductDescription;
                            softwareProduct.LogoUri = sp.LogoUri;
                            softwareProduct.Status = sp.Status;

                            softwareProduct.LegalEntityId = legalEntity.LegalEntityId;
                            softwareProduct.LegalEntityName = legalEntity.LegalEntityName;
                            softwareProduct.LegalEntityStatus = legalEntity.Status;

                            softwareProduct.BrandId = dataRecipientBrand.DataRecipientBrandId;
                            softwareProduct.BrandName = dataRecipientBrand.BrandName;
                            softwareProduct.BrandStatus = dataRecipientBrand.Status;

                            softwareProducts.Add(softwareProduct);
                        }
                    }
                }
            }

            return softwareProducts;
        }

        private async Task<string> GetData(string endpoint, int version)
        {            
            _logger.LogInformation("Retrieving data from {endpoint} (x-v: {version})...", endpoint, version);
            
            var httpClient = GetHttpClient();
            httpClient.DefaultRequestHeaders.Add("x-v", version.ToString());
            var response = await httpClient.GetAsync(endpoint);

            _logger.LogInformation("Status code: {statusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }
        
        private async Task<ValidationResult> ValidateClientId()
        {
            var clientIdClaimValue = this.User.Claims.FirstOrDefault(c => c.Type == ClaimNames.ClientId);
            if (clientIdClaimValue == null 
                || (await _clientService.Get(clientIdClaimValue.Value) == null))
            {
                return ValidationResult.Fail(ErrorCodes.InvalidRequest, "The client is unknown");
            }

            return ValidationResult.Pass();
        }

        private static HttpClient GetHttpClient()
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            return new HttpClient(clientHandler);
        }
    }
}
