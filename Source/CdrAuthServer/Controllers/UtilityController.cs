using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    [Route("utility")]
    public class UtilityController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<UtilityController> _logger;
        private readonly IGrantService _grantService;
        private readonly IClientService _clientService;
        private readonly HttpClient _httpClient;

        public UtilityController(
            IConfiguration config,
            ILogger<UtilityController> logger,
            HttpClient httpClient,
            IGrantService grantService,
            IClientService clientService)
        {
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
            _grantService = grantService;
            _clientService = clientService;
        }

        /// <summary>
        /// This controller method is provided to trigger an arrangement revocation at a data recipient.
        /// Normally, this would be done from the DH dashboard.  
        /// However, until a dashboard is in place this method can be used to trigger a request.
        /// </summary>
        /// <returns>IActionResult</returns>
        /// <remarks>
        /// Note: this controller action would not be implemented in a production system and is provided for testing purposes.
        /// </remarks>
        [HttpGet]
        [Route("dr/revoke-arrangement-jwt/{cdrArrangementId}")]
        public async Task<IActionResult> TriggerDataRecipientArrangementRevocationByJwt(string cdrArrangementId)
        {
            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                _logger.LogError("cdrArrangementId is null or empty");
                return new UnprocessableEntityObjectResult(new CdsError("urn:au-cds:error:cds-all:Authorisation/InvalidArrangement", "Invalid Consent Arrangement", cdrArrangementId));
            }

            var result = await SendRevocationRequest(cdrArrangementId);
            if (result.StatusCode != 204)
            {
                return new UnprocessableEntityObjectResult(result);
            }

            return NoContent();
        }

        private async Task<ValidationResult> SendRevocationRequest(string cdrArrangementId)
        {
            // Find the CDR Arrangement Grant.
            var grant = await _grantService.Get(GrantTypes.CdrArrangement, cdrArrangementId);

            // "cdr_arrangement_grant" grant not found for given id. 
            if (grant == null)
            {
                _logger.LogError("cdrArrangementId:{id} not found to revoke", cdrArrangementId);
                return ValidationResult.Fail(ErrorCodes.InvalidRequest, "Invalid cdr_arrangement_id");
            }

            // Find the associated client id.
            var client = await _clientService.Get(grant.ClientId);
            if (client == null)
            {
                _logger.LogError("client with Id:{id} in the grant, not found", grant.ClientId);
                return ValidationResult.Fail(ErrorCodes.InvalidClient, "Invalid client_id");
            }

            // Build the parameters for the call to the DR's arrangement revocation endpoint.
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var revocationUri = new Uri($"{client.RecipientBaseUri}/arrangements/revoke");
            var brandId = configOptions.BrandId;
            var jwtPayload = new JwtPayload(
               claims: new Claim[]
               {
                 new Claim(ClaimNames.Subject, brandId),
                 new Claim(ClaimNames.JwtId, Guid.NewGuid().ToString()),
               },
               issuer: brandId,
               audience: revocationUri.ToString(),
               notBefore: DateTime.UtcNow,
               issuedAt: DateTime.UtcNow,
               expires: DateTime.UtcNow.AddMinutes(5));
            var signedBearerTokenJwt = await GetSignedJwt(jwtPayload);

            _logger.LogInformation("Calling DR arrangement revocation endpoint ({revocationUri})...", revocationUri);

            // Call the DR's arrangement revocation endpoint.
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signedBearerTokenJwt);
            var formValues = await GetFormValues(cdrArrangementId, brandId, revocationUri.ToString());
            var httpResponse = await _httpClient.PostAsync(revocationUri, new FormUrlEncodedContent(formValues));

            _logger.LogInformation("Response from DR arrangement revocation endpoint: {httpResponse}", httpResponse);

            if (httpResponse.IsSuccessStatusCode)
            {
                return ValidationResult.Pass(204);
            }

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ValidationResult.Fail(ErrorCodes.UnauthorizedClient, $"Unauthorization error when calling {revocationUri}");
            }

            var respError = JsonConvert.DeserializeObject<CdsErrorList>(await httpResponse.Content.ReadAsStringAsync());
            if (respError?.Errors.Any() is true)
            {
                var error = respError.Errors.First();
                return ValidationResult.Fail(error.Code, error.Detail);
            }

            return ValidationResult.Fail(ErrorCodes.InvalidRequest, $"An error occurred calling {revocationUri}");
        }

        private async Task<string> GetSignedJwt(JwtPayload jwtPayload)
        {
            var cert = await _config.GetPS256SigningCertificate();
            var signingCredentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256);

            var jwtHeader = new JwtHeader(
                signingCredentials: signingCredentials,
                outboundAlgorithmMap: null,
                tokenType: TokenTypes.Jwt);

            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(jwt);
        }

        private async Task<Dictionary<string, string>> GetFormValues(
            string cdrArrangementId,
            string brandId,
            string audience)
        {
            var formValues = new Dictionary<string, string>();
            var jwt = new JwtPayload(
                issuer: brandId,
                audience: audience,
                notBefore: DateTime.UtcNow,
                issuedAt: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(5),
                claims: new Claim[] {
                    new Claim(ClaimNames.CdrArrangementId, cdrArrangementId),
                    new Claim(ClaimNames.Subject, brandId),
                    new Claim(ClaimNames.JwtId, Guid.NewGuid().ToString())
                });

            formValues.Add("cdr_arrangement_jwt", (await GetSignedJwt(jwt)));

            _logger.LogInformation("Arrangement revocation request using {form}:{form_value} ", formValues.First().Key, formValues.First().Value);

            return formValues;
        }
    }
}
