using System.Net.Http.Headers;
using CdrAuthServer.Configuration;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using static CdrAuthServer.Domain.Constants;
using static CdrAuthServer.Services.IConsentRevocationService;

namespace CdrAuthServer.Services
{
    /// <summary>
    /// DH initiated consent revocation functionality.
    /// </summary>
    /// <param name="httpClient">The managed http client.</param>
    /// <param name="configurationOptions">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public class ConsentRevocationService(HttpClient httpClient, IOptions<ConfigurationOptions> configurationOptions, ICertificateLoader certificateLoader, ILogger<ConsentRevocationService> logger) : IConsentRevocationService
    {
        private readonly ConfigurationOptions _configurationOptions = configurationOptions.Value;
        private readonly Task<SigningCredentials> signingCredentialsTask = CreateSigningCredentials(configurationOptions, certificateLoader);

        /// <summary>
        /// Create signing credentials from configuration.
        /// </summary>
        /// <param name="configurationOptions">The configuration which will be used to fetch the PS256 signing certificate details.</param>
        /// <param name="certificateLoader">The certificate loader to read the certificate based on the configuration.</param>
        /// <returns>A signing credentials generated from the certificate specified in configuration.</returns>
        private static async Task<SigningCredentials> CreateSigningCredentials(IOptions<ConfigurationOptions> configurationOptions, ICertificateLoader certificateLoader)
        {
            var certificate = await certificateLoader.Load(configurationOptions.Value.PS256SigningCertificate!);
            return new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);
        }

        /// <inheritdoc />
        public async Task<OutboundCallDetails> RevokeAdrArrangement(Client client, string arrangementId, TimeSpan revocationTimeout, CancellationToken cancellationToken = default)
        {
            Exception? exception = null;
            HttpResponseMessage? response = null;
            var request = await PopulateRequestMessageForRevocationCall(client, arrangementId);

            using (var requestTimeout = new CancellationTokenSource(revocationTimeout))
            {
                // Combine the cancellation token with the incoming one so that if that is cancelled before the timeout it's also handled.
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, cancellationToken);

                try
                {
                    response = await httpClient.SendAsync(request, linkedCancellation.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            return new OutboundCallDetails(request, response, exception);
        }

        /// <summary>
        /// Creates the request message for revoking the arrangement.
        /// </summary>
        /// <param name="client">The client details.</param>
        /// <param name="cdrArrangementId">The arrangement to revoke.</param>
        /// <returns>The request message populated with the appropriate payload and bearer token.</returns>
        private async Task<HttpRequestMessage> PopulateRequestMessageForRevocationCall(Client client, string cdrArrangementId)
        {
            // Build the parameters for the call to the DR's arrangement revocation endpoint.
            var revocationUri = new Uri($"{client.RecipientBaseUri}/arrangements/revoke");
            var brandId = _configurationOptions.BrandId;

            var signedBearerTokenJwt = await GenerateBearerToken(brandId, revocationUri.ToString());
            logger.LogDebug("Bearer {Token}", signedBearerTokenJwt);

            var formValues = await GetRequestPayload(cdrArrangementId, brandId, revocationUri.ToString());
            var urlEncodedContent = new FormUrlEncodedContent(formValues);

            HttpRequestMessage revokeRequest = new(HttpMethod.Post, revocationUri) { Content = urlEncodedContent };
            revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signedBearerTokenJwt);

            return revokeRequest;
        }

        /// <summary>
        /// Builds the form values with the arrangement JWT that needs to be revoked.
        /// </summary>
        /// <param name="cdrArrangementId">The identifier of the arrangement to be revoked.</param>
        /// <param name="brandId">The brand.</param>
        /// <param name="audience">The audience.</param>
        /// <returns>The request payload with arrangement JWT.</returns>
        private async Task<Dictionary<string, string>> GetRequestPayload(
            string cdrArrangementId,
            string brandId,
            string audience)
        {
            var arrangementJwt = await GenerateCdrArrangementJwt(cdrArrangementId, brandId, audience);
            logger.LogDebug("Arrangement {ArrangementJWT}", arrangementJwt);

            var formValues = new Dictionary<string, string>
            {
                { "cdr_arrangement_jwt", arrangementJwt },
            };

            return formValues;
        }

        /// <summary>
        /// Generates a JWT representing the CDR arrangement that is to be revoked.
        /// </summary>
        /// <param name="cdrArrangementId">The identifier of the arrangement to be revoked.</param>
        /// <param name="brandId">The brand.</param>
        /// <param name="audience">The audience.</param>
        /// <returns>The CDR arrangement JWT.</returns>
        private async Task<string> GenerateCdrArrangementJwt(string cdrArrangementId, string brandId, string audience)
        {
            var handler = new JsonWebTokenHandler();
            var signingCredentials = await signingCredentialsTask;
            var descriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
                {
                    { ClaimNames.CdrArrangementId, cdrArrangementId },
                    { JwtRegisteredClaimNames.Sub, brandId },
                    { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() },
                },
                Issuer = brandId,
                Audience = audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = signingCredentials,
            };

            return handler.CreateToken(descriptor);
        }

        /// <summary>
        /// Generates a bearer token for the revocation request.
        /// </summary>
        /// <param name="brandId">The brand.</param>
        /// <param name="audience">The audience.</param>
        /// <returns>The bearer token for the revocation request.</returns>
        private async Task<string> GenerateBearerToken(string brandId, string audience)
        {
            var handler = new JsonWebTokenHandler();
            var signingCredentials = await signingCredentialsTask;
            var descriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>
                {
                    { JwtRegisteredClaimNames.Sub, brandId },
                    { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() },
                },
                Issuer = brandId,
                Audience = audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = signingCredentials,
            };

            return handler.CreateToken(descriptor);
        }
    }
}
