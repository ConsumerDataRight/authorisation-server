using System.Security.Cryptography.X509Certificates;
using CdrAuthServer.Configuration;
using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Certificates;
using Microsoft.IdentityModel.Tokens;

namespace CdrAuthServer.Extensions
{
    public static class ConfigExtensions
    {
        private static IConfiguration? _config;
        private static ConfigurationOptions _configurationOptions = null!;

        public static ConfigurationOptions GetConfigurationOptions(this IConfiguration config, HttpContext? context = null)
        {
            _config = config;
            _configurationOptions = new ConfigurationOptions();
            config.GetSection("CdrAuthServer").Bind(_configurationOptions);
            SetDefaults(context);
            return _configurationOptions;
        }

        public static async Task<X509Certificate2?> GetPS256SigningCertificate(this IConfiguration config)
        {
            var options = config.GetConfigurationOptions();
            var loader = new CertificateLoader();

            if (options.PS256SigningCertificate is not null)
            {
                return await loader.Load(options.PS256SigningCertificate);
            }

            return null;
        }

        public static async Task<X509Certificate2?> GetES256SigningCertificate(this IConfiguration config)
        {
            var options = config.GetConfigurationOptions();
            var loader = new CertificateLoader();

            if (options.ES256SigningCertificate is not null)
            {
                return await loader.Load(options.ES256SigningCertificate);
            }

            return null;
        }

        private static void SetDefaults(HttpContext? context)
        {
            // Default endpoints.
            var basePath = (context != null && context.Request.PathBase.HasValue) ? context.Request.PathBase.Value : _configurationOptions.BasePath;
            var baseUri = $"{_configurationOptions.BaseUri}{basePath}";
            var secureBaseUri = $"{_configurationOptions.SecureBaseUri}{basePath}";
            _configurationOptions.Issuer = SetDefault(_configurationOptions.Issuer, baseUri);
            _configurationOptions.JwksUri = SetDefault(_configurationOptions.JwksUri, $"{baseUri}/.well-known/openid-configuration/jwks");
            _configurationOptions.RegistrationEndpoint = SetDefault(_configurationOptions.RegistrationEndpoint, $"{secureBaseUri}/connect/register");
            _configurationOptions.AuthorizationEndpoint = SetDefault(_configurationOptions.AuthorizationEndpoint, $"{baseUri}/connect/authorize");
            _configurationOptions.TokenEndpoint = SetDefault(_configurationOptions.TokenEndpoint, $"{secureBaseUri}/connect/token");
            _configurationOptions.UserinfoEndpoint = SetDefault(_configurationOptions.UserinfoEndpoint, $"{secureBaseUri}/connect/userinfo");
            _configurationOptions.IntrospectionEndpoint = SetDefault(_configurationOptions.IntrospectionEndpoint, $"{secureBaseUri}/connect/introspect");
            _configurationOptions.ArrangementRevocationEndpoint = SetDefault(_configurationOptions.ArrangementRevocationEndpoint, $"{secureBaseUri}/connect/arrangements/revoke");
            _configurationOptions.RevocationEndpoint = SetDefault(_configurationOptions.RevocationEndpoint, $"{secureBaseUri}/connect/revocation");
            _configurationOptions.PushedAuthorizationEndpoint = SetDefault(_configurationOptions.PushedAuthorizationEndpoint, $"{secureBaseUri}/connect/par");

            _configurationOptions.DefaultAcrValue = SetDefault(_configurationOptions.DefaultAcrValue, "urn:cds.au:cdr:2");
            _configurationOptions.AcrValuesSupported = SetDefault(_configurationOptions.AcrValuesSupported, [
                "urn:cds.au:cdr:2"
            ]);
            _configurationOptions.ClaimsSupported = SetDefault(_configurationOptions.ClaimsSupported, [
                "name",
                "given_name",
                "family_name",
                "sharing_duration",
                "iss",
                "sub",
                "aud",
                "acr",
                "exp",
                "iat",
                "nonce",
                "auth_time",
                "updated_at",
            ]);
            _configurationOptions.CodeChallengeMethodsSupported = SetDefault(_configurationOptions.CodeChallengeMethodsSupported, [
                "S256"
            ]);
            _configurationOptions.GrantTypesSupported = SetDefault(_configurationOptions.GrantTypesSupported, [
                "authorization_code",
                "refresh_token",
                "client_credentials"
            ]);
            var scopesSupported = _configurationOptions.ScopesSupported;
            if (_configurationOptions.ScopesProfile == ConfigurationOptions.ScopesProfileAll || _configurationOptions.ScopesProfile == ConfigurationOptions.ScopesProfileBanking)
            {
                scopesSupported = scopesSupported!.Union(_configurationOptions.BankingScopesSupported!).ToList();
            }

            if (_configurationOptions.ScopesProfile == ConfigurationOptions.ScopesProfileAll || _configurationOptions.ScopesProfile == ConfigurationOptions.ScopesProfileEnergy)
            {
                scopesSupported = scopesSupported!.Union(_configurationOptions.EnergyScopesSupported!).ToList();
            }

            _configurationOptions.ScopesSupported = SetDefault(scopesSupported, [
                "openid",
                "profile",
                "cdr:registration"
            ]);
            _configurationOptions.ResponseModesSupported = SetDefault(_configurationOptions.ResponseModesSupported, [
                "jwt",
            ]);
            _configurationOptions.SubjectTypesSupported = SetDefault(_configurationOptions.SubjectTypesSupported, [
                "pairwise"
            ]);
            _configurationOptions.RequestObjectSigningAlgValuesSupported = SetDefault(_configurationOptions.RequestObjectSigningAlgValuesSupported, [
                SecurityAlgorithms.RsaSsaPssSha256,
                SecurityAlgorithms.EcdsaSha256
            ]);
            _configurationOptions.TokenEndpointAuthMethodsSupported = SetDefault(_configurationOptions.TokenEndpointAuthMethodsSupported, [
                "private_key_jwt",
            ]);
            _configurationOptions.TokenEndpointAuthSigningAlgValuesSupported = SetDefault(_configurationOptions.TokenEndpointAuthSigningAlgValuesSupported, [
                SecurityAlgorithms.RsaSsaPssSha256,
                SecurityAlgorithms.EcdsaSha256
            ]);
            _configurationOptions.IdTokenSigningAlgValuesSupported = SetDefault(_configurationOptions.IdTokenSigningAlgValuesSupported, [
                SecurityAlgorithms.RsaSsaPssSha256,
                SecurityAlgorithms.EcdsaSha256
            ]);
            _configurationOptions.AuthorizationSigningAlgValuesSupported = SetDefault(_configurationOptions.AuthorizationSigningAlgValuesSupported, [
                SecurityAlgorithms.RsaSsaPssSha256,
                SecurityAlgorithms.EcdsaSha256
            ]);
            _configurationOptions.AuthorizationEncryptionAlgValuesSupported = SetDefault(
                _configurationOptions.AuthorizationEncryptionAlgValuesSupported,
                "RSA-OAEP,RSA-OAEP-256");
            _configurationOptions.AuthorizationEncryptionEncValuesSupported = SetDefault(
                _configurationOptions.AuthorizationEncryptionEncValuesSupported,
                "A128CBC-HS256,A256GCM");
            _configurationOptions.BrandId = SetDefault(_configurationOptions.BrandId, "00000000-0000-0000-0000-000000000000");
            _configurationOptions.HeadlessMode = SetDefault(_configurationOptions.HeadlessMode, false);
            _configurationOptions.HeadlessModeRequiresConfirmation = SetDefault(_configurationOptions.HeadlessModeRequiresConfirmation, false);
            _configurationOptions.ValidateResourceEndpoint = SetDefault(_configurationOptions.ValidateResourceEndpoint, true);
            _configurationOptions.AllowDuplicateRegistrations = SetDefault(_configurationOptions.AllowDuplicateRegistrations, false);
            _configurationOptions.SupportJarmEncryption = SetDefault(_configurationOptions.SupportJarmEncryption, false);
            _configurationOptions.RequestUriExpirySeconds = SetDefault(_configurationOptions.RequestUriExpirySeconds, 90);
            _configurationOptions.AccessTokenExpirySeconds = SetDefault(_configurationOptions.AccessTokenExpirySeconds, 300);
            _configurationOptions.IdTokenExpirySeconds = SetDefault(_configurationOptions.IdTokenExpirySeconds, 300);
            _configurationOptions.UseMtlsEndpointAliases = SetDefault(_configurationOptions.UseMtlsEndpointAliases, false);
            _configurationOptions.ClockSkewSeconds = SetDefault(_configurationOptions.ClockSkewSeconds, 0);
            _configurationOptions.ClientCertificateThumbprintHttpHeaderName = SetDefault(_configurationOptions.ClientCertificateThumbprintHttpHeaderName, HttpHeaders.ClientCertificateThumbprint);
            _configurationOptions.ClientCertificateCommonNameHttpHeaderName = SetDefault(_configurationOptions.ClientCertificateCommonNameHttpHeaderName, HttpHeaders.ClientCertificateCommonName);
            _configurationOptions.ClientCertificateHttpHeaderName = SetDefault(_configurationOptions.ClientCertificateHttpHeaderName, HttpHeaders.ClientCertificate);

            // If needing to turn off mtls checking at specific endpoints, such as for FAPI JARM testing with PAR endpoint.
            var endpointList = _config?.GetValue<string>("CdrAuthServer:OverrideMtlsCheckEndpointList", string.Empty);
            _configurationOptions.OverrideMtlsChecks = SetDefault(_configurationOptions.OverrideMtlsChecks, endpointList?.Split(',') ?? []);
        }

        private static string SetDefault(string option, string defaultValue)
        {
            if (string.IsNullOrEmpty(option))
            {
                return defaultValue;
            }

            return option;
        }

        private static int SetDefault(int? option, int defaultValue)
        {
            if (!option.HasValue || option.Value == 0)
            {
                return defaultValue;
            }

            return option.Value;
        }

        private static bool SetDefault(bool? option, bool defaultValue)
        {
            if (!option.HasValue || !option.Value)
            {
                return defaultValue;
            }

            return option.Value;
        }

        private static IList<string> SetDefault(IList<string>? option, string[] defaultValues)
        {
            if (option == null || option.Count == 0)
            {
                return defaultValues;
            }

            return option;
        }
    }
}
