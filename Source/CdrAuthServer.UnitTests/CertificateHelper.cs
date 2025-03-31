using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.UnitTests
{
    /// <summary>
    /// Helper functionality for creating certificates for testing.
    /// </summary>
    public static class CertificateHelper
    {
        /// <summary>
        /// Creates the <see cref="X509Certificate2"/> for signing.
        /// </summary>
        /// <param name="cn">The common name for the subject.</param>
        /// <returns>A self-signed certificate for testing.</returns>
        public static X509Certificate2 CreateSigning(string cn = "signing")
        {
            using var rsa = RSA.Create();

            var req = new CertificateRequest($"cn={cn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(1));

            return cert;
        }
    }
}
