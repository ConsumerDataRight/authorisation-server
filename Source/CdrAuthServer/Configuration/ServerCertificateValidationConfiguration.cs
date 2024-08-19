namespace CdrAuthServer.Configuration
{
    public class ServerCertificateValidationConfiguration
    {
        public bool Enabled { get; set; }
        public string RootCertificate { get; set; } = string.Empty;
        public string IntermediateCertificate { get; set; } = string.Empty;
    }
}
