using System.Net;
using CdrAuthServer.Domain.Models;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using CdrAuthServer.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    [Route("utility")]
    public class UtilityController : ControllerBase
    {
        private readonly ILogger<UtilityController> _logger;
        private readonly IGrantService _grantService;
        private readonly IClientService _clientService;
        private readonly IConsentRevocationService _consentRevocationService;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        public UtilityController(
            ILogger<UtilityController> logger,
            IGrantService grantService,
            IClientService clientService,
            IConsentRevocationService consentRevocationService)
        {
            _logger = logger;
            _grantService = grantService;
            _clientService = clientService;
            _consentRevocationService = consentRevocationService;
        }

        /// <summary>
        /// This controller method is provided to  delete the arrangement and refreshtoken in authserver and
        /// trigger an arrangement revocation at a data recipient.
        /// Normally, this would be done from the DH dashboard.
        /// However, until a dashboard is in place this method can be used to trigger a request.
        /// </summary>
        /// <returns>IActionResult.</returns>
        /// <remarks>
        /// Note: this controller action would not be implemented in a production system and is provided for testing purposes.
        /// </remarks>
        [HttpGet]
        [Route("dr/revoke-arrangement-jwt/{cdrArrangementId}")]
        [ApiVersionNeutral]
        public async Task<IActionResult> RemoveArrangementAndTriggerDataRecipientArrangementRevocation(string cdrArrangementId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(cdrArrangementId))
            {
                _logger.LogError("cdrArrangementId is null or empty");
                return BadRequest(new ResponseErrorList().AddInvalidField(nameof(cdrArrangementId)));
            }

            var (adrRevocationResponse, cdsErrorList, httpStatusCode) = await RemoveArrangementAndSendRevocationRequestAsync(cdrArrangementId, cancellationToken);

            if (cdsErrorList?.Errors.Count > 0)
            {
                return StatusCode((int)httpStatusCode, cdsErrorList);
            }

            var jsonResponse = JsonConvert.SerializeObject(
                adrRevocationResponse,
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include, ContractResolver = new CamelCasePropertyNamesContractResolver() });

            return Ok(jsonResponse);
        }

        private static AdrArrangementRevocationResponse GetRevocationResponse(ArrangeRevocationRequest? request, ArrangeRevocationResponse? response)
        {
            return new()
            {
                ArrangeRevocationRequest = request,
                ArrangeRevocationResponse = response,
            };
        }

        private async Task<(AdrArrangementRevocationResponse? AdrRevocationResponse, ResponseErrorList? CdsErrorList, HttpStatusCode StatusCode)> RemoveArrangementAndSendRevocationRequestAsync(string cdrArrangementId, CancellationToken cancellationToken)
        {
            HttpResponseMessage? revocationResponseMessage;
            ArrangeRevocationRequest? revocationRequestInfo;
            string? errorMessage = null;

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
                return (null, new ResponseErrorList { Errors = { new CdsError(ErrorCodes.Cds.InvalidConsentArrangement, "Invalid client_id", grant.ClientId) } }, HttpStatusCode.InternalServerError);
            }

            // Revoke the arrangement locally
            await RemoveArrangement(cdrArrangementId, client, (CdrArrangementGrant)grant);

            // Notify the data recipient
            (var revocationRequestMessage, revocationResponseMessage, var exception) = await _consentRevocationService.RevokeAdrArrangement(client, cdrArrangementId, _timeout, cancellationToken);

            // Add request and response details sent to ADR so that the invoker of this endpoint has this information for it's own logs.
            revocationRequestInfo = await GetAdrRevokeRequestInfoAsync(revocationRequestMessage);

            if (exception is not null)
            {
                _logger.LogError(exception, "Error revoking arrangement {ExceptionMessage}", exception.Message);

                // Change the error message when the request was cancelled due to timeout being exceeded or client cancelling the request.
                errorMessage = exception is TaskCanceledException
                    ? $"The operation was cancelled as the ADR did not respond within the timeout period of {_timeout.Seconds} seconds."
                    : exception.Message;
            }

            return (GetRevocationResponse(revocationRequestInfo, await GetAdrRevokeResponseInfoAsync(revocationResponseMessage, errorMessage)), null, HttpStatusCode.OK);
        }

        // Remove arrangement and token from AuthServer DB
        private async Task RemoveArrangement(string cdrArrangementId, Client client, CdrArrangementGrant cdrArrangementGrant)
        {
            // Delete the grants.
            await _grantService.Delete(client.ClientId, GrantTypes.RefreshToken, cdrArrangementGrant.RefreshToken ?? string.Empty);
            await _grantService.Delete(client.ClientId, GrantTypes.CdrArrangement, cdrArrangementGrant.Key);
            _logger.LogInformation("Removed arrangement {ArrangementId} and refresh token for client {ClientId}", cdrArrangementId, client.ClientId);
        }

        private static async Task<ArrangeRevocationRequest> GetAdrRevokeRequestInfoAsync(HttpRequestMessage requestMessage)
        {
            return new ArrangeRevocationRequest
            {
                Body = await (requestMessage.Content?.ReadAsStringAsync() ?? Task.FromResult(string.Empty)),
                Headers = requestMessage.Headers?.ToDictionary(a => a.Key, a => a.Value).ToJson(),
                ContentType = requestMessage.Content?.Headers.ContentType?.MediaType ?? null,
                Url = requestMessage.RequestUri?.ToString() ?? null,
                Method = requestMessage.Method.ToString(),
            };
        }

        private static async Task<ArrangeRevocationResponse> GetAdrRevokeResponseInfoAsync(HttpResponseMessage? httpResponse, string? responseContent = null)
        {
            var response = new ArrangeRevocationResponse
            {
                Content = responseContent ?? await (httpResponse?.Content.ReadAsStringAsync() ?? Task.FromResult(string.Empty)),
                Headers = httpResponse?.Headers.ToDictionary(a => a.Key, a => a.Value).ToJson(),
                StatusCode = (int?)httpResponse?.StatusCode,
            };

            return response;
        }
    }
}
