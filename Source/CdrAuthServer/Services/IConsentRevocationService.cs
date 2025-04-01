using CdrAuthServer.Models;

namespace CdrAuthServer.Services
{
    /// <summary>
    /// DH initiated consent revocation functionality.
    /// </summary>
    public interface IConsentRevocationService
    {
        /// <summary>
        /// Represents the details of an outbound call.
        /// </summary>
        /// <param name="Request">The request that was sent / attempted.</param>
        /// <param name="Response">The response that was received (if no exception was thrown).</param>
        /// <param name="Exception">The exception if one was thrown.</param>
        public record OutboundCallDetails(HttpRequestMessage Request, HttpResponseMessage? Response, Exception? Exception);

        /// <summary>
        /// Revokes ADR Arrangement.
        /// </summary>
        /// <param name="client">The client details.</param>
        /// <param name="arrangementId">The arrangement to revoke.</param>
        /// <param name="revocationTimeout">The timeout for the revocation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The request and response, as well as an exception if one was thrown.
        /// </returns>
        Task<OutboundCallDetails> RevokeAdrArrangement(Client client, string arrangementId, TimeSpan revocationTimeout, CancellationToken cancellationToken = default);
    }
}
