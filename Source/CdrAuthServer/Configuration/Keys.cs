namespace CdrAuthServer.Configuration
{
    public static class Keys
    {
        public const string CdrAuthServer = "CdrAuthServer";
        public const string BaseUri = "CdrAuthServer:BaseUri";
        public const string SecureBaseUri = "CdrAuthServer:SecureBaseUri";
        public const string BasePath = "CdrAuthServer:BasePath";
        public const string BasePathExpression = "CdrAuthServer:BasePathExpression";
        public const string MetadataAddress = "CdrAuthServer:MetadataAddress";
        public const string Issuer = "CdrAuthServer:Issuer";
        public const string ClockSkewSeconds = "CdrAuthServer:ClockSkewSeconds";
        public const string AcrValuesSupported = "CdrAuthServer:AcrValuesSupported";
        public const string ClaimsSupported = "CdrAuthServer:ClaimsSupported";
        public const string CodeChallengeMethodsSupported = "CdrAuthServer:CodeChallengeMethodsSupported";
        public const string GrantTypesSupported = "CdrAuthServer:GrantTypesSupported";
        public const string ResponseModesSupported = "CdrAuthServer:ResponseModesSupported";
        public const string ResponseTypesSupported = "CdrAuthServer:ResponseTypesSupported";
        public const string SubjectTypesSupported = "CdrAuthServer:SubjectTypesSupported";
        public const string RequestObjectSigningAlgValuesSupported = "CdrAuthServer:RequestObjectSigningAlgValuesSupported";
        public const string TokenEndpointAuthMethodsSupported = "CdrAuthServer:TokenEndpointAuthMethodsSupported";
        public const string TokenEndpointAuthSigningAlgValuesSupported = "CdrAuthServer:TokenEndpointAuthSigningAlgValuesSupported";
        public const string IdTokenSigningAlgValuesSupported = "CdrAuthServer:IdTokenSigningAlgValuesSupported";
        public const string IdTokenEncryptionAlgValuesSupported = "CdrAuthServer:IdTokenEncryptionAlgValuesSupported";
        public const string IdTokenEncryptionEncValuesSupported = "CdrAuthServer:IdTokenEncryptionEncValuesSupported";
        public const string PS256SigningCertificate = "CdrAuthServer:PS256SigningCertificate";
        public const string ES256SigningCertificate = "CdrAuthServer:ES256SigningCertificate";
        public const string SeedDataFilePath = "CdrAuthServer:SeedData:FilePath";        
    }
}
