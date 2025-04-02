namespace CdrAuthServer.Infrastructure.Certificates
{
    public enum CertificateSource
    {
        Raw,
        File,
        Url,
        KeyVault,
    }

    public class CertificateLoadDetails
    {
        public CertificateSource Source { get; set; }

        public string Content { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
