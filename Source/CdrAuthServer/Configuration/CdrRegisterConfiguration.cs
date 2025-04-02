namespace CdrAuthServer.Configuration
{
    public class CdrRegisterConfiguration
    {
        public string SsaJwksUri { get; set; } = string.Empty;

        public bool CheckSoftwareProductStatus { get; set; } = true;

        public string GetDataRecipientsEndpoint { get; set; } = string.Empty;

        public int Version { get; set; }
    }
}
