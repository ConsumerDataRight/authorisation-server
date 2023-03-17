using CdrAuthServer.Authorisation;
using CdrAuthServer.Configuration;
using CdrAuthServer.Domain;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly ILogger<RegistrationController> _logger;
        private readonly IConfiguration _config;
        private readonly IClientRegistrationValidator _clientRegistrationValidator;
        private readonly IClientService _clientService;

        public RegistrationController(
            IConfiguration config,
            ILogger<RegistrationController> logger,
            IClientService clientService,
            IClientRegistrationValidator clientRegistrationValidator)
        {
            _logger = logger;
            _config = config;
            _clientService = clientService;
            _clientRegistrationValidator = clientRegistrationValidator;
        }

        [HttpGet]
        [Route("connect/register/{clientId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthorisationPolicy.Registration)]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRegistration(string clientId)
        {
            // Check that the clientId provided on the route matches the access token.
            var clientIdResult = await ValidateClientId(clientId);
            if (!clientIdResult.IsValid)
            {
                _logger.LogInformation("Validation failed - {@clientIdResult}", clientIdResult);
                Response.Headers.Append(
                    HttpHeaders.WWWAuthenticate,
                    $"Bearer error=\"{clientIdResult.Error}\", error_description=\"{clientIdResult.ErrorDescription}\"");
                return new UnauthorizedObjectResult(new { error = clientIdResult.Error, error_description = clientIdResult.ErrorDescription });
            }

            var client = await _clientService.Get(clientId);
            return new JsonResult(client, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        [HttpPost]
        [Route("connect/register")]
        [ValidateMtls]
        [Consumes("application/jwt")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRegistration(ClientRegistrationRequest request)
        {
            // Validate the registration request.
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var result = await _clientRegistrationValidator.Validate(request, configOptions);
            if (!result.IsValid)
            {
                return new BadRequestObjectResult(new Error(result.Error, result.ErrorDescription));
            }

            // Check if the software product has already been registered.
            if (!configOptions.AllowDuplicateRegistrations)
            {
                var existingClient = await _clientService.GetClientBySoftwareProductId(request.SoftwareStatement.SoftwareId);
                if (existingClient != null)
                {
                    return ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.DUPLICATE_REGISTRATION);
                }
            }

            var client = Map(request, Guid.NewGuid().ToString(), configOptions);
            client.ClientIdIssuedAt = DateTime.UtcNow.ToEpoch();
            client.Scope = FilterScopes(client.Scope, configOptions);
            var clientRegistrationResponse = await _clientService.Create(client);

            return new JsonResult(clientRegistrationResponse, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }) { StatusCode = StatusCodes.Status201Created };
        }

        [HttpPut]
        [Route("connect/register/{clientId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthorisationPolicy.Registration)]
        [Consumes("application/jwt")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateRegistration(
            [Required] string clientId,
            ClientRegistrationRequest request)
        {
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);

            // Check that the clientId provided on the route matches the access token.
            var clientIdResult = await ValidateClientId(clientId);
            if (!clientIdResult.IsValid)
            {
                _logger.LogError("client_id validation failed with error:{error} errordescription:{desc}", clientIdResult.Error, clientIdResult.ErrorDescription);

                Response.Headers.Append(
                    HttpHeaders.WWWAuthenticate,
                    $"Bearer error=\"{clientIdResult.Error}\", error_description=\"{clientIdResult.ErrorDescription}\"");
                return new UnauthorizedObjectResult(new { error = clientIdResult.Error, error_description = clientIdResult.ErrorDescription });
            }

            // Validate the registration request.
            var result = await _clientRegistrationValidator.Validate(request, configOptions);
            if (!result.IsValid)
            {
                _logger.LogError("client registration validation failed with error:{error} errordescription:{desc}", clientIdResult.Error, clientIdResult.ErrorDescription);
                return new BadRequestObjectResult(new Error(result.Error, result.ErrorDescription));
            }

            var client = Map(request, clientId, configOptions);
            client.Scope = FilterScopes(client.Scope, configOptions);
            var clientRegistrationResponse = await _clientService.Update(client);

            return new JsonResult(clientRegistrationResponse, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }) { StatusCode = StatusCodes.Status200OK };
        }

        [HttpDelete]
        [Route("connect/register/{clientId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [PolicyAuthorize(AuthorisationPolicy.Registration)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteRegistration([Required] string clientId)
        {
            // Check that the clientId provided on the route matches the access token.
            var clientIdResult = await ValidateClientId(clientId);
            if (!clientIdResult.IsValid)
            {
                _logger.LogError("client_id validation failed with error:{error} errordescription:{desc}", clientIdResult.Error, clientIdResult.ErrorDescription);

                Response.Headers.Append(
                    HttpHeaders.WWWAuthenticate,
                    $"Bearer error=\"{clientIdResult.Error}\", error_description=\"{clientIdResult.ErrorDescription}\"");
                return new UnauthorizedObjectResult(new { error = clientIdResult.Error, error_description = clientIdResult.ErrorDescription });
            }

            await _clientService.Delete(clientId);

            return NoContent();
        }

        private string FilterScopes(string scope, ConfigurationOptions configOptions)
        {
            var scopes = scope.Split(' ');
            return string.Join(" ", scopes.Where(s => configOptions?.ScopesSupported?.Contains(s) is true || configOptions?.ClientCredentialScopesSupported?.Contains(s) is true));
        }

        private async Task<Validation.ValidationResult> ValidateClientId(string clientId)
        {
            var clientIdClaimValue = this.User.Claims.FirstOrDefault(c => c.Type == ClaimNames.ClientId);
            if (clientIdClaimValue == null
            || (!clientId.Equals(clientIdClaimValue.Value, StringComparison.OrdinalIgnoreCase))
            || (await _clientService.Get(clientId) == null))
            {
                return Validation.ValidationResult.Fail(ErrorCodes.InvalidRequest, "The client is unknown");
            }

            return Validation.ValidationResult.Pass();
        }

        private Client Map(ClientRegistrationRequest request, string clientId, ConfigurationOptions configOptions)
        {
            var client = new Client()
            {
                ClientId = clientId,
                ClientName = request.SoftwareStatement.ClientName ?? string.Empty,
                ClientDescription = request.SoftwareStatement.ClientDescription ?? string.Empty,
                ApplicationType = request.ApplicationType ?? "web",
                ClientUri = request.SoftwareStatement.ClientUri ?? string.Empty,
                GrantTypes = request.GrantTypes,
                ResponseTypes = request.ResponseTypes,
                RedirectUris = (request.RedirectUris == null || !request.RedirectUris.Any()) ? request.SoftwareStatement.RedirectUris : request.RedirectUris,
                Scope = request.SoftwareStatement.Scope ?? "openid profile cdr:registration",
                LegalEntityId = request.SoftwareStatement.LegalEntityId ?? string.Empty,
                LegalEntityName = request.SoftwareStatement.LegalEntityName ?? string.Empty,
                OrgId = request.SoftwareStatement.OrgId ?? string.Empty,
                OrgName = request.SoftwareStatement.OrgName ?? string.Empty,
                LogoUri = request.SoftwareStatement.LogoUri ?? string.Empty,
                TosUri = request.SoftwareStatement.TosUri ?? string.Empty,
                PolicyUri = request.SoftwareStatement.PolicyUri ?? string.Empty,
                RecipientBaseUri = request.SoftwareStatement.RecipientBaseUri ?? string.Empty,
                SectorIdentifierUri = request.SoftwareStatement.SectorIdentifierUri ?? null,
                RevocationUri = request.SoftwareStatement.RevocationUri ?? string.Empty,
                JwksUri = request.SoftwareStatement.JwksUri ?? string.Empty,
                SoftwareId = request.SoftwareStatement.SoftwareId ?? string.Empty,
                RequestObjectSigningAlg = request.RequestObjectSigningAlg ?? configOptions.RequestObjectSigningAlgValuesSupported.First(),
                IdTokenEncryptedResponseAlg = request.IdTokenEncryptedResponseAlg,
                IdTokenEncryptedResponseEnc = request.IdTokenEncryptedResponseEnc,
                IdTokenSignedResponseAlg = request.IdTokenSignedResponseAlg ?? configOptions.IdTokenSigningAlgValuesSupported.First(),
                TokenEndpointAuthMethod = request.TokenEndpointAuthMethod ?? configOptions.TokenEndpointAuthMethodsSupported.First(),
                TokenEndpointAuthSigningAlg = request.TokenEndpointAuthSigningAlg ?? configOptions.TokenEndpointAuthSigningAlgValuesSupported.First(),
                SoftwareStatementJwt = request.SoftwareStatementJwt ?? string.Empty,
                SoftwareRoles = request.SoftwareStatement.SoftwareRoles ?? string.Empty
            };

            client.AuthorizationSignedResponseAlg = null;
            client.AuthorizationEncryptedResponseAlg = null;
            client.AuthorizationEncryptedResponseEnc = null;

            // Handle JARM settings when using ACF.
            if (request.ResponseTypes.Contains(ResponseTypes.AuthCode))
            {
                client.AuthorizationSignedResponseAlg = request.AuthorizationSignedResponseAlg;

                // If JARM encryption is supported.
                if (configOptions.SupportJarmEncryption)
                {
                    client.AuthorizationEncryptedResponseAlg = String.IsNullOrEmpty(request.AuthorizationEncryptedResponseAlg) ? configOptions.AuthorizationEncryptionAlgValuesSupportedList.FirstOrDefault() : request.AuthorizationEncryptedResponseAlg;
                    client.AuthorizationEncryptedResponseEnc = String.IsNullOrEmpty(request.AuthorizationEncryptedResponseEnc) ? configOptions.AuthorizationEncryptionEncValuesSupportedList.FirstOrDefault() : request.AuthorizationEncryptedResponseEnc;
                }
            }

            return client;
        }
    }
}
