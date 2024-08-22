using CdrAuthServer.Domain.Models;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure.Models;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
        /// This controller method is provided to  delete the arrangement and refreshtoken in authserver and
        /// trigger an arrangement revocation at a data recipient.
        /// Normally, this would be done from the DH dashboard.  
        /// However, until a dashboard is in place this method can be used to trigger a request.
        /// </summary>
        /// <returns>IActionResult</returns>
        /// <remarks>
        /// Note: this controller action would not be implemented in a production system and is provided for testing purposes.
        /// </remarks>
        [HttpGet]
        [Route("dr/revoke-arrangement-jwt/{cdrArrangementId}")]
        [ApiVersionNeutral]
        public async Task<IActionResult> RemoveArrangementAndTriggerDataRecipientArrangementRevocation(string cdrArrangementId)
        {
            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                _logger.LogError("cdrArrangementId is null or empty");
                return BadRequest(new ResponseErrorList().AddInvalidField(nameof(cdrArrangementId)));
            }

            var (adrRevocationResponse, cdsErrorList, httpStatusCode) = await RemoveArrangementAndSendRevocationRequestAsync(cdrArrangementId);

            if (cdsErrorList?.Errors.Count!=0)
            {
                return StatusCode((int)httpStatusCode, cdsErrorList);
            }

            var jsonResponse = JsonConvert.SerializeObject(adrRevocationResponse, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include, ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return Ok(jsonResponse);
        }

        private static AdrArrangementRevocationResponse GetRevocationResponse(ArrangeRevocationRequest? request, ArrangeRevocationResponse? response)
        {
            return new()
            {
                ArrangeRevocationRequest = request,
                ArrangeRevocationResponse = response
            };
        }

        private async Task<(AdrArrangementRevocationResponse?, ResponseErrorList?, HttpStatusCode statusCode)> RemoveArrangementAndSendRevocationRequestAsync(string cdrArrangementId)
        {
            HttpResponseMessage? httpResponse = null;

            // Find the CDR Arrangement Grant.
            var grant = await _grantService.Get(GrantTypes.CdrArrangement, cdrArrangementId);

            // "cdr_arrangement_grant" grant not found for given id. 
            if (grant == null)
            {
                _logger.LogError("cdrArrangementId:{Id} not found to revoke", cdrArrangementId);
                return (null, new ResponseErrorList().AddInvalidField(nameof(cdrArrangementId)), HttpStatusCode.BadRequest);
            }

            // Find the associated client id.
            var client = await _clientService.Get(grant.ClientId);
            if (client == null)
            {
                _logger.LogError("client with Id:{Id} in the grant, not found", grant.ClientId);
                return (null, new ResponseErrorList { Errors = { new CdsError(Domain.Constants.ErrorCodes.Cds.InvalidConsentArrangement, "Invalid client_id", grant.ClientId) } }, HttpStatusCode.InternalServerError);
            }

            //get the httprequest with the values populated
            var (revokeRequest, urlEncodedContent) = await PopulateRequestMessageForRevocationCall(client, cdrArrangementId);

            using (revokeRequest)
            {
                //add request and response details sent to ADR to custom headers.
                var revocRequestInfo = await GetAdrRevokeRequestInfoAsync(revokeRequest, _httpClient.DefaultRequestHeaders, urlEncodedContent);

                try
                {
                    //Revoke the arrangement before notifying the Data Recipient
                    await RemoveArrangement(cdrArrangementId, client, (CdrArrangementGrant)grant);

                    // Call the DR's arrangement revocation endpoint.
                    using var cts = new CancellationTokenSource(new TimeSpan(0, 0, 30));
                    httpResponse = await _httpClient.SendAsync(revokeRequest, cts.Token).ConfigureAwait(false);

                    _logger.LogInformation("Response from DR arrangement revocation endpoint: {HttpResponse}", httpResponse);

                    return (GetRevocationResponse(revocRequestInfo, await GetAdrRevokeResponseInfoAsync(httpResponse)), null, HttpStatusCode.OK);
                }
                catch (TaskCanceledException ex)
                {
                    //return custom message when the httpclient timesout after set timeout of 30seconds instead of
                    //ex.Message of task canceled message.
                    _logger.LogError(ex, "Error revoking arrangement {ExceptionMessage}", ex.Message);

                    var errorMessage = "The operation was cancelled as the ADR did not respond within the timeout period of 30 seconds.";

                    return (GetRevocationResponse(revocRequestInfo, await GetAdrRevokeResponseInfoAsync(httpResponse, errorMessage)), null, HttpStatusCode.OK);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revoking arrangement {ExceptionMessage}", ex.Message);
                    return (GetRevocationResponse(revocRequestInfo, await GetAdrRevokeResponseInfoAsync(httpResponse, ex.Message)), null, HttpStatusCode.OK);
                }
            }

        }

        //Remove arrangement and token from AuthServer DB
        private async Task RemoveArrangement(string cdrArrangementId, Client client, CdrArrangementGrant cdrArrangementGrant)
        {
            // Delete the grants.
            await _grantService.Delete(client.ClientId, GrantTypes.RefreshToken, cdrArrangementGrant.RefreshToken ?? "");
            await _grantService.Delete(client.ClientId, GrantTypes.CdrArrangement, cdrArrangementGrant.Key);
            _logger.LogInformation("Removed arrangement {Arrangmentid} and refresh token for client {Clientid}", cdrArrangementId, client.ClientId);
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
                    new(ClaimNames.CdrArrangementId, cdrArrangementId),
                    new(ClaimNames.Subject, brandId),
                    new(ClaimNames.JwtId, Guid.NewGuid().ToString())
                });

            formValues.Add("cdr_arrangement_jwt", (await GetSignedJwt(jwt)));

            _logger.LogInformation("Arrangement revocation request using {Form}:{Form_value} ", formValues.First().Key, formValues.First().Value);

            return formValues;
        }

        private static async Task<ArrangeRevocationRequest> GetAdrRevokeRequestInfoAsync(HttpRequestMessage requestMessage, HttpHeaders requestHeaders, FormUrlEncodedContent? urlEncodeContent)
        {
            return new ArrangeRevocationRequest
            {
                Body = await (urlEncodeContent?.ReadAsStringAsync() ?? Task.FromResult("")),
                Headers = JsonConvert.SerializeObject(requestHeaders.ToDictionary(a => a.Key, a => a.Value)) ?? null,
                ContentType = requestMessage.Content?.Headers.ContentType?.MediaType ?? null,
                Url = requestMessage.RequestUri?.ToString() ?? null,
                Method = requestMessage.Method.ToString()
            };
        }

        private static async Task<ArrangeRevocationResponse> GetAdrRevokeResponseInfoAsync(HttpResponseMessage? httpResponse, string? responseContent = null)
        {
            var response = new ArrangeRevocationResponse
            {
                Content = responseContent ?? await (httpResponse?.Content.ReadAsStringAsync() ?? Task.FromResult("")),
                Headers = httpResponse == null ? null : JsonConvert.SerializeObject(httpResponse.Headers.ToDictionary(a => a.Key, a => a.Value)),
                StatusCode = httpResponse?.StatusCode.ToJson()
            };

            return response;
        }

        private async Task<(HttpRequestMessage, FormUrlEncodedContent?)> PopulateRequestMessageForRevocationCall(Client client, string cdrArrangementId)
        {
            // Build the parameters for the call to the DR's arrangement revocation endpoint.
            var configOptions = _config.GetConfigurationOptions(this.HttpContext);
            var revocationUri = new Uri($"{client.RecipientBaseUri}/arrangements/revoke");
            var brandId = configOptions.BrandId;
            var jwtPayload = new JwtPayload(
               claims: new Claim[]
               {
                     new(ClaimNames.Subject, brandId),
                     new(ClaimNames.JwtId, Guid.NewGuid().ToString()),
               },
               issuer: brandId,
               audience: revocationUri.ToString(),
               notBefore: DateTime.UtcNow,
               issuedAt: DateTime.UtcNow,
               expires: DateTime.UtcNow.AddMinutes(5));
            var signedBearerTokenJwt = await GetSignedJwt(jwtPayload);

            _logger.LogInformation("Calling DR arrangement revocation endpoint ({RevocationUri})...", revocationUri);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signedBearerTokenJwt);
            var formValues = await GetFormValues(cdrArrangementId, brandId, revocationUri.ToString());
            var urlEncodedContent = new FormUrlEncodedContent(formValues);
            
            //Build the httprequestmessage with all the parameters.
            HttpRequestMessage revokeRequest = new(HttpMethod.Post, revocationUri) { Content = urlEncodedContent };

            return (revokeRequest, urlEncodedContent);
        }
    }
}
