﻿using System.Security.Cryptography.X509Certificates;

namespace CdrAuthServer.mTLS.Gateway.Certificates
{
    /// <summary>
    /// Validates that a client certificate has been issued by the Mock CDR CA.
    /// </summary>
    public class CertificateValidator : ICertificateValidator
    {
        private readonly ILogger<CertificateValidator> _logger;
        private readonly IConfiguration _config;

        public CertificateValidator(ILogger<CertificateValidator> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public void ValidateClientCertificate(X509Certificate2 clientCert)
        {
            _logger.LogInformation("Validating certificate within the {CertificateValidator}", nameof(CertificateValidator));

            if (clientCert == null)
            {
                throw new ArgumentNullException(nameof(clientCert));
            }

            var rootCertLocation = _config.GetValue<string>("Certificates:RootCACertificate:Location");
            if (string.IsNullOrEmpty(rootCertLocation))
            {
                throw new InvalidOperationException("Root certificate location is not configured.");
            }

            // Validate that the certificate has been issued by the Mock CDR CA.
            var rootCACertificate = new X509Certificate2(rootCertLocation);
            _logger.LogDebug("Validating client certificate using: {RootCACertificate}", rootCACertificate);

            var ch = new X509Chain();
            ch.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            ch.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            ch.ChainPolicy.CustomTrustStore.Clear();
            ch.ChainPolicy.CustomTrustStore.Add(rootCACertificate);
            ch.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

            try
            {
                ch.Build(clientCert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred validating the client certificate");
                throw new ClientCertificateException("The certificate chain cannot be discovered from the provided client certificate.", ex);
            }

            if (ch.ChainStatus.Any())
            {
                _logger.LogError("An error occurred validating the client certificate: {Status}", ch.ChainStatus[0].StatusInformation);
                throw new ClientCertificateException(ch.ChainStatus[0].StatusInformation);
            }
        }
    }
}
