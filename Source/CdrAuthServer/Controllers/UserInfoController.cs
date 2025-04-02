using CdrAuthServer.Authorisation;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure.Authorisation;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        private readonly ILogger<UserInfoController> _logger;
        private readonly IClientService _clientService;
        private readonly ICdrService _cdrService;
        private readonly ICustomerService _customerService;
        private readonly IConfiguration _config;

        public UserInfoController(
            IConfiguration config,
            ILogger<UserInfoController> logger,
            IClientService clientService,
            ICdrService cdrService,
            ICustomerService customerService)
        {
            _config = config;
            _logger = logger;
            _clientService = clientService;
            _cdrService = cdrService;
            _customerService = customerService;
        }

        [HttpPost]
        [HttpGet]
        [Route("connect/userinfo")]
        [ApiVersionNeutral]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthServerAuthorisationPolicyAttribute.UserInfo)]
        public async Task<IActionResult> GetUserInfo()
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);

            var client = await _clientService.Get(User.GetClientId());
            if (client == null)
            {
                _logger.LogInformation("Client not found {@User}", User);
                return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.CLIENT_NOT_FOUND);
            }

            // Check software product status (if configured).
            if (configOptions.CdrRegister != null && configOptions.CdrRegister.CheckSoftwareProductStatus)
            {
                var softwareProductId = client.SoftwareId;
                var softwareProduct = await _cdrService.GetSoftwareProduct(softwareProductId);
                if (softwareProduct == null)
                {
                    _logger.LogInformation("Software Product not found {SoftwareProductId}", softwareProductId);
                    return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.SOFTWARE_PRODUCT_NOT_FOUND);
                }

                if (!softwareProduct.IsActive())
                {
                    _logger.LogInformation("Software product status is removed - consents cannot be revoked {SoftwareProductId}", softwareProductId);
                    return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.SOFTWARE_PRODUCT_STATUS_INACTIVE, softwareProduct.GetStatusDescription());
                }
            }

            var userInfo = new UserInfo()
            {
                Audience = User.GetClientId() ?? string.Empty,
                Issuer = User.GetIssuer() ?? string.Empty,
                Subject = User.GetSubject() ?? string.Empty,
            };

            if (configOptions.HeadlessMode)
            {
                var user = new HeadlessModeUser();
                userInfo.FamilyName = user.FamilyName;
                userInfo.GivenName = user.GivenName;
                userInfo.Name = user.Name;

                return new JsonResult(userInfo);
            }
            else
            {
                var subjectId = User?.GetSubject()?
                                    .DecryptSub(client, _config);

                // Get customer login details from seed data file instead
                var customer = await _customerService.Get(subjectId ?? string.Empty);
                userInfo.FamilyName = customer.FamilyName;
                userInfo.GivenName = customer.GivenName;
                userInfo.Name = customer.Name;

                return new JsonResult(userInfo);
            }
        }
    }
}
