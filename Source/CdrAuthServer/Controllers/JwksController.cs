using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CdrAuthServer.Extensions;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CdrAuthServer.Controllers
{
    [ApiController]
    public class JwksController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwksController> _logger;
        private readonly IMemoryCache _cache;

        public JwksController(
            IConfiguration config,
            ILogger<JwksController> logger,
            IMemoryCache cache)
        {
            _config = config;
            _logger = logger;
            _cache = cache;
        }

        [EnableCors("AllOrigins")]
        [HttpGet]
        [Route("/.well-known/openid-configuration/jwks")]
        [ApiVersionNeutral]
        public async Task<JsonResult> GetJwks()
        {
            return new JsonResult(await GenerateJwks(), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        /// <summary>
        /// Generate a JWKS for the Auth Server.
        /// </summary>
        /// <returns>JsonWebKeySet</returns>
        private async Task<Models.JsonWebKeySet> GenerateJwks()
        {
            _logger.LogInformation("{JwksController}.{GenerateJwks}", nameof(JwksController), nameof(GenerateJwks));

            string cacheKey = "jwks";
            var item = _cache.Get<Models.JsonWebKeySet>(cacheKey);

            if (item != null)
            {
                _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
                return item;
            }

            var ps256jwk = await GetPS256Jwk();
            var es256jwk = await GetES256Jwk();

            var jwks = new Models.JsonWebKeySet()
            {
                keys = [ps256jwk, es256jwk]
            };

            // Add the jwks to the cache.
            _cache.Set<Models.JsonWebKeySet>(cacheKey, jwks, absoluteExpiration: DateTimeOffset.Now.AddMinutes(_config.GetValue<int>("CacheExpiryMinutes", 5)));
            _logger.LogInformation("JWKS added to cache");

            return jwks;
        }

        private async Task<Models.JsonWebKey> GetPS256Jwk()
        {
            var cert = await _config.GetPS256SigningCertificate();

            // Get credentials from certificate
            var securityKey = new X509SecurityKey(cert);
            var signingCredentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256);
            var rsaParams = signingCredentials.Certificate.GetRSAPublicKey().ExportParameters(false);
            var e = Base64UrlEncoder.Encode(rsaParams.Exponent);
            var n = Base64UrlEncoder.Encode(rsaParams.Modulus);

            var jwk = new Models.JsonWebKey()
            {
                use = "sig",
                alg = "PS256",
                kty = "RSA",
                kid = signingCredentials.Kid,
                n = n,
                e = e,
                x5t = securityKey.X5t,
                x5c = [Convert.ToBase64String(cert.RawData)]
            };
            return jwk;
        }

        private async Task<Models.JsonWebKey> GetES256Jwk()
        {
            var cert = await _config.GetES256SigningCertificate();

            var ecdsa = cert.GetECDsaPrivateKey();
            var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = cert.Thumbprint };
            var parameters = ecdsa.ExportParameters(false);
            var x = Base64UrlEncoder.Encode(parameters.Q.X);
            var y = Base64UrlEncoder.Encode(parameters.Q.Y);

            var jwk = new Models.JsonWebKey
            {
                use = "sig",
                alg = "ES256",
                kty = "EC",
                kid = securityKey.KeyId,
                x = x,
                y = y,
                crv = GetCrvValue(parameters.Curve),
                x5t = Base64UrlEncoder.Encode(cert.GetCertHash()),
                x5c = new string[] { Convert.ToBase64String(cert.RawData) }
            };

            return jwk;
        }

        private string GetCrvValue(ECCurve curve)
        {
            switch (curve.Oid.Value)
            {
                case "1.2.840.10045.3.1.7":
                    return JsonWebKeyECTypes.P256;
                case "1.3.132.0.34":
                    return JsonWebKeyECTypes.P384;
                case "1.3.132.0.35":
                    return JsonWebKeyECTypes.P521;
            };

            _logger.LogError("Unsupported curve type of {Value} - {FriendlyName}", curve.Oid.Value, curve.Oid.FriendlyName);

            throw new InvalidOperationException($"Unsupported curve type of {curve.Oid.Value} - {curve.Oid.FriendlyName}");
        }

    }
}
