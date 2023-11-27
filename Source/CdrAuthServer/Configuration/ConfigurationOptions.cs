using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Certificates;

namespace CdrAuthServer.Configuration
{
    public class ConfigurationOptions
    {
        public string BrandId { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string BrandAbn { get; set; } = string.Empty;
        public string BaseUri { get; set; } = string.Empty;
        public string SecureBaseUri { get; set; } = string.Empty;
        public string BasePath { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string JwksUri { get; set; } = string.Empty;
        public string RegistrationEndpoint { get; set; } = string.Empty;
        public string AuthorizationEndpoint { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = string.Empty;
        public string UserinfoEndpoint { get; set; } = string.Empty;
        public string IntrospectionEndpoint { get; set; } = string.Empty;
        public string ArrangementRevocationEndpoint { get; set; } = string.Empty;
        public string RevocationEndpoint { get; set; } = string.Empty;
        public string PushedAuthorizationEndpoint { get; set; } = string.Empty;
        public string DefaultAcrValue { get; set; } = string.Empty;
        public IList<string>? AcrValuesSupported { get; set; }
        public IList<string>? ClaimsSupported { get; set; }
        public IList<string>? CodeChallengeMethodsSupported { get; set; }
        public IList<string>? GrantTypesSupported { get; set; }
        public string ScopesProfile { get; set; } = string.Empty;
        public IList<string>? ScopesSupported { get; set; }
        public IList<string>? BankingScopesSupported { get; set; }
        public IList<string>? EnergyScopesSupported { get; set; }
        public IList<string>? ClientCredentialScopesSupported { get; set; }
        public IList<string>? ResponseModesSupported { get; set; }
        public IList<string>? ResponseTypesSupported { get; set; }
        public IList<string>? SubjectTypesSupported { get; set; }
        public IList<string>? RequestObjectSigningAlgValuesSupported { get; set; }
        public IList<string>? TokenEndpointAuthMethodsSupported { get; set; }
        public IList<string>? TokenEndpointAuthSigningAlgValuesSupported { get; set; }
        public IList<string>? IdTokenSigningAlgValuesSupported { get; set; }
        public IList<string>? IdTokenEncryptionAlgValuesSupported { get; set; }
        public IList<string>? IdTokenEncryptionEncValuesSupported { get; set; }
        public bool AlwaysEncryptIdTokens { get; set; }
        public bool UseMtlsEndpointAliases { get; set; }
        public IList<string>? AuthorizationSigningAlgValuesSupported { get; set; }

        public string AuthorizationEncryptionAlgValuesSupported { get; set; } = string.Empty;
        public IList<string>? AuthorizationEncryptionAlgValuesSupportedList
        {
            get
            {
                return string.IsNullOrEmpty(AuthorizationEncryptionAlgValuesSupported) ? null
                    : AuthorizationEncryptionAlgValuesSupported.Split(',', StringSplitOptions.TrimEntries);
            }
        }

        public string AuthorizationEncryptionEncValuesSupported { get; set; } = string.Empty;
        public IList<string>? AuthorizationEncryptionEncValuesSupportedList
        {
            get
            {
                return string.IsNullOrEmpty(AuthorizationEncryptionEncValuesSupported) ? null
                    : AuthorizationEncryptionEncValuesSupported.Split(',', StringSplitOptions.TrimEntries);
            }
        }

        public CertificateLoadDetails? PS256SigningCertificate { get; set; }
        public CertificateLoadDetails? ES256SigningCertificate { get; set; }
        public int RequestUriExpirySeconds { get; set; } = 90;
        public int AccessTokenExpirySeconds { get; set; } = 300;
        public int IdTokenExpirySeconds { get; set; } = 300;
        public int ClockSkewSeconds { get; set; } = 0;
        public CdrRegisterConfiguration? CdrRegister { get; set; }
        public bool HeadlessMode { get; set; }
        public bool HeadlessModeRequiresConfirmation { get; set; }
        public bool ValidateResourceEndpoint { get; set; } = true;
        public bool AllowDuplicateRegistrations { get; set; } = false;
        public bool SupportJarmEncryption { get; set; } = false;
        public string ClientCertificateThumbprintHttpHeaderName { get; set; } = HttpHeaders.ClientCertificateThumbprint;
        public string ClientCertificateCommonNameHttpHeaderName { get; set; } = HttpHeaders.ClientCertificateCommonName;
        public string ClientCertificateHttpHeaderName { get; set; } = HttpHeaders.ClientCertificate;
        public IList<string>? OverrideMtlsChecks { get; set; }
        public string AutoFillCustomerId { get; set; } = string.Empty;
        public string AutoFillOtp { get; set; } = string.Empty;
        public string AuthUiBaseUri { get; set; } = string.Empty;
        public const string scopesProfileAll = "all";
        public const string scopesProfileBanking = "banking";
        public const string scopesProfileEnergy = "energy";
    }
}
