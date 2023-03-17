using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#nullable enable

namespace CdrAuthServer.IntegrationTests
{
    public class API
    {
        public HttpMethod? Method;
        public string? URL;
        public string? AccessToken;

        public string? XV { get; set; }
        public string? XMinV { get; set; }
        public string? IfNoneMatch { get; set; }

        public string? ClientCertificateFilename { get; set; }
        public string? ClientCertificatePassword { get; set; }

        /// <summary>
        /// Set authentication header explicity. Can't be used if AccessToken is set
        /// </summary>
        public AuthenticationHeaderValue? AuthenticationHeaderValue;
        public HttpContent? RequestContent;

        public async Task<HttpResponseMessage> SendAsync()
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // Add client certificate
            if (ClientCertificateFilename != null)
            {
                clientHandler.ClientCertificates.Add(new X509Certificate2(
                    ClientCertificateFilename,
                    ClientCertificatePassword,
                    X509KeyStorageFlags.Exportable
                ));
            }

            var client = new HttpClient(clientHandler);

            var request = new HttpRequestMessage(
                Method ?? throw new ArgumentNullException(nameof(Method)),
                URL ?? throw new ArgumentNullException(nameof(URL))
            );

            // Set x-v header if provided
            if (XV != null)
            {
                request.Headers.Add("x-v", XV);
            }

            // Set x-min-v header if provided
            if (XMinV != null)
            {
                request.Headers.Add("x-min-v", XMinV);
            }

            // Set If-None-Match header if provided
            if (IfNoneMatch != null)
            {
                request.Headers.Add("If-None-Match", $"\"{IfNoneMatch}\"");
            }

            // Attach access token if provided
            if (AccessToken != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }

            // Set AuthenticationHeaderValue explicity
            if (AuthenticationHeaderValue != null)
            {
                if (AccessToken != null)
                {
                    throw new Exception($"{nameof(API)}.{nameof(SendAsync)} - Can't use both AccessToken and AuthenticationHeaderValue.");
                }

                request.Headers.Authorization = AuthenticationHeaderValue;
            }

            // Attach content if provided
            if (RequestContent != null)
            {
                request.Content = RequestContent;
            }

            BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);            

            var response = await client.SendAsync(request);

            return response;
        }
    }
}
