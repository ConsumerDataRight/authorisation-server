using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CdrAuthServer.Infrastructure.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CdrAuthServer.Infrastructure.Extensions
{
    public static class CertificateExtensions
    {
        public static string GetOCSPUrlFromCertificate(this X509Certificate2 certificate)
        {
            X509Extension? aiaExtension = certificate.Extensions["1.3.6.1.5.5.7.1.1"]; // AuthorityInfoAccess

            if (aiaExtension == null)
            {
                throw new ClientCertificateException("Unable to validate certificate - Missing Authority Information Access");
            }

            string ocspResponderUrl = string.Empty;

            var aiaData = new AsnEncodedData(aiaExtension.Oid, aiaExtension.RawData);
            string aiaString = aiaData.Format(true);

            // Look for the OCSP URL in the AuthorityInfoAccess string
            string[] lines = aiaString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string lineLower = line.ToLower();
                if (lineLower.Contains("ocsp"))
                {
                    int urlStartIndex = lineLower.IndexOf("http");
                    if (urlStartIndex >= 0)
                    {
                        ocspResponderUrl = lineLower.Substring(urlStartIndex).Trim();
                        break;
                    }
                }
            }

            if (ocspResponderUrl == string.Empty)
            {
                throw new ClientCertificateException("Unable to validate certificate - Missing OCSP URL");
            }

            return ocspResponderUrl;
        }
    }
}
