using CdrAuthServer.Configuration;
using CdrAuthServer.Models.Json;
using CdrAuthServer.Models.Register;
using Microsoft.Extensions.Options;

namespace CdrAuthServer.Services
{
    /// <summary>
    /// Provides functionality for interacting with CDR register.
    /// </summary>
    public class RegisterClientService(HttpClient httpClient, IOptions<CdrRegisterConfiguration> cdrRegisterOptions) : IRegisterClientService
    {
        private readonly CdrRegisterConfiguration _cdrRegisterOptions = cdrRegisterOptions.Value;

        /// <inheritdoc />
        public async Task<RegisterResponse<LegalEntity>?> GetDataRecipients(CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _cdrRegisterOptions.GetDataRecipientsEndpoint);
            request.Headers.Add("x-v", _cdrRegisterOptions.Version.ToString());

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<RegisterResponse<LegalEntity>>(cancellationToken: cancellationToken);
        }
    }
}
