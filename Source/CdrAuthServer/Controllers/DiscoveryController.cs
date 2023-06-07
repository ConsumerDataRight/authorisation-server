using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class DiscoveryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DiscoveryController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [Route(".well-known/openid-configuration")]
        public JsonResult GetDiscoveryDocument()
        {
            var configOptions = _configuration.GetConfigurationOptions(this.HttpContext);
            var model = new Discovery
            {
                Issuer = configOptions.Issuer,
                JwksUri = configOptions.JwksUri,
                RegistrationEndpoint = configOptions.RegistrationEndpoint,
                AuthorizationEndpoint = configOptions.AuthorizationEndpoint,
                TokenEndpoint = configOptions.TokenEndpoint,
                UserinfoEndpoint = configOptions.UserinfoEndpoint,
                IntrospectionEndpoint = configOptions.IntrospectionEndpoint,
                ArrangementRevocationEndpoint = configOptions.ArrangementRevocationEndpoint,
                RevocationEndpoint = configOptions.RevocationEndpoint,
                PushedAuthorizationEndpoint = configOptions.PushedAuthorizationEndpoint,
                AcrValuesSupported = configOptions.AcrValuesSupported,
                ClaimsParameterSupported = true,
                ClaimsSupported = configOptions.ClaimsSupported,
                CodeChallengeMethodsSupported = configOptions.CodeChallengeMethodsSupported,
                GrantTypesSupported = configOptions.GrantTypesSupported,
                ScopesSupported = configOptions.ScopesSupported.Union(configOptions.ClientCredentialScopesSupported).ToList(),
                ResponseModesSupported = configOptions.ResponseModesSupported,
                ResponseTypesSupported = configOptions.ResponseTypesSupported,
                SubjectTypesSupported = configOptions.SubjectTypesSupported,
                RequirePushedAuthorizationRequests = true,
                RequestParameterSupported = false,
                RequestUriParameterSupported = true,
                RequestObjectSigningAlgValuesSupported = configOptions.RequestObjectSigningAlgValuesSupported,
                TlsClientCertificateBoundAccessTokens = true,
                TokenEndpointAuthMethodsSupported = configOptions.TokenEndpointAuthMethodsSupported,
                TokenEndpointAuthSigningAlgValuesSupported = configOptions.TokenEndpointAuthSigningAlgValuesSupported,
                IdTokenSigningAlgValuesSupported = configOptions.IdTokenSigningAlgValuesSupported,
                IdTokenEncryptionAlgValuesSupported = configOptions.IdTokenEncryptionAlgValuesSupported,
                IdTokenEncryptionEncValuesSupported = configOptions.IdTokenEncryptionEncValuesSupported,
                AuthorizationSigningAlgValuesSupported  = configOptions.AuthorizationSigningAlgValuesSupported,
            };

            if (configOptions.SupportJarmEncryption)
            {
                model.AuthorizationEncryptionAlgValuesSupported = configOptions.AuthorizationEncryptionAlgValuesSupportedList;
                model.AuthorizationEncryptionEncValuesSupported = configOptions.AuthorizationEncryptionEncValuesSupportedList;
            }

            // Switch to out the "mtls_endpoint_aliases" property in the discovery document.
            if (configOptions.UseMtlsEndpointAliases) 
            {
                model.MtlsEndpointAliases = new Dictionary<string, string>
                {
                    { "token_endpoint", model.TokenEndpoint },
                    { "revocation_endpoint", model.RevocationEndpoint },
                    { "introspection_endpoint", model.IntrospectionEndpoint },
                    { "pushed_authorization_request_endpoint", model.PushedAuthorizationEndpoint },
                    { "registration_endpoint", model.RegistrationEndpoint },
                    { "userinfo_endpoint", model.UserinfoEndpoint }
                };

                // Set the mTLS endpoints back to their TLS equivalents.
                model.TokenEndpoint = model.TokenEndpoint.Replace(configOptions.SecureBaseUri, configOptions.BaseUri);
                model.RevocationEndpoint = model.RevocationEndpoint.Replace(configOptions.SecureBaseUri, configOptions.BaseUri);
                model.IntrospectionEndpoint = model.IntrospectionEndpoint.Replace(configOptions.SecureBaseUri, configOptions.BaseUri);
                model.PushedAuthorizationEndpoint = model.PushedAuthorizationEndpoint.Replace(configOptions.SecureBaseUri, configOptions.BaseUri);
                model.RegistrationEndpoint = model.RegistrationEndpoint.Replace(configOptions.SecureBaseUri, configOptions.BaseUri);
                model.UserinfoEndpoint = model.UserinfoEndpoint.Replace(configOptions.SecureBaseUri, configOptions.BaseUri);
            }

            return new JsonResult(model);
        }
    }
}
