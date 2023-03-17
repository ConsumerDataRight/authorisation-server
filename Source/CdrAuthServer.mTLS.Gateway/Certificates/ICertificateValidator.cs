using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.mTLS.Gateway.Certificates
{
    public interface ICertificateValidator
    {
        void ValidateClientCertificate(X509Certificate2 clientCert);
    }
}
