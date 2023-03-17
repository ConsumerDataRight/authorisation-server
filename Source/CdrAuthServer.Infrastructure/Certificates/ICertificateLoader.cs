using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.Infrastructure.Certificates
{
    public interface ICertificateLoader
    {
        public Task<X509Certificate2> Load(IConfiguration config, string configurationKey);

        public Task<X509Certificate2> Load(CertificateLoadDetails loadDetails);
    }
}
