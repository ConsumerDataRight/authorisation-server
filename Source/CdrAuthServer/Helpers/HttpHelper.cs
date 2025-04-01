using CdrAuthServer.Configuration;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.Helpers
{
    public static class HttpHelper
    {
        public static HttpClientHandler CreateHttpClientHandler(IConfiguration configuration)
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback(configuration),
            };
        }

        public static HttpClientHandler CreateHttpClientHandler(bool isServerCertificateValidationEnabled)
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback(isServerCertificateValidationEnabled),
            };
        }

        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback(IConfiguration configuration)
        {
            bool isServerCertificateValidationEnabled = configuration.GetValue<bool>(Keys.IsServerCertificateValidationEnabled);
            return ServerCertificateCustomValidationCallback(isServerCertificateValidationEnabled);
        }

        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback(
            bool isServerCertificateValidationEnabled)
        {
            return (message, serverCert, chain, errors) =>
            {
                if (!isServerCertificateValidationEnabled)
                {
                    return true;
                }

                return errors == SslPolicyErrors.None;
            };
        }
    }
}
