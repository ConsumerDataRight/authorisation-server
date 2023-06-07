using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.Infrastructure.Certificates
{
    public class CertificateLoader : ICertificateLoader
    {
        public async Task<X509Certificate2> Load(IConfiguration config, string configurationKey)
        {
            var certificateLoadDetails = new CertificateLoadDetails();
            config.GetSection(configurationKey).Bind(certificateLoadDetails);
            return await Load(certificateLoadDetails);
        }

        public async Task<X509Certificate2> Load(CertificateLoadDetails loadDetails)
        {
            switch (loadDetails.Source)
            {
                case CertificateSource.File:
                    return LoadCertificateFromBytes(File.ReadAllBytes(loadDetails.Location), loadDetails.Password);

                case CertificateSource.Url:
                    return LoadCertificateFromBytes((await DownloadData(loadDetails.Location)), loadDetails.Password);

                case CertificateSource.Raw:
                    return LoadCertificateFromBytes(Convert.FromBase64String(loadDetails.Content), loadDetails.Password);

                case CertificateSource.KeyVault:                    
                    throw new NotImplementedException();

                default:
                    throw new ConfigurationErrorsException("Invalid certificate source");
            }
        }

        private static X509Certificate2 LoadCertificateFromBytes(byte[] certBytes, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new X509Certificate2(certBytes);
            }

            return new X509Certificate2(certBytes, password, X509KeyStorageFlags.Exportable);
        }

        private async static Task<byte[]> DownloadData(string url)
        {
            using (var http = new HttpClient())
            {
                return await http.GetByteArrayAsync(url);
            }
        }
    }
}
