using CdrAuthServer.Infrastructure.Exceptions;
using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.Infrastructure.Extensions
{
    public static class CertificateExtensions
    {
        public static string GetOCSPUrlFromCertificate(this X509Certificate2 certificate)
        {
            X509Extension ocspExtension = certificate.Extensions["1.3.6.1.5.5.7.1.1"]; //AuthorityInfoAccess

            if (ocspExtension == null)
            {
                throw new ClientCertificateException("Unable to validate certificate - Missing Authority Information Access");
            }

            // Extract the OCSP responder URL from the extension data
            // Assuming the extensionData contains the URL as a string
            string extensionData = System.Text.Encoding.ASCII.GetString(ocspExtension.RawData);

            string ocspResponderUrl = string.Empty;
            if (extensionData.Contains("http"))
            {
                int idx = extensionData.IndexOf("http");
                ocspResponderUrl = extensionData.Substring(idx);
            }

            if (ocspResponderUrl == string.Empty)
            {
                throw new ClientCertificateException("Unable to validate certificate - Missing OCSP URL");
            }

            return ocspResponderUrl;                        

        }
    }
}
