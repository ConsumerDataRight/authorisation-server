using CdrAuthServer.Exceptions;
using CdrAuthServer.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace CdrAuthServer.Services
{
    public class JwksService : IJwksService
    {
        private readonly ILogger<JwksService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public JwksService(
            ILogger<JwksService> logger,
            IConfiguration config,
            HttpClient httpClient,
            IMemoryCache cache)
        {
            _logger = logger;
            _config = config;
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> GetJwks(Uri jwksUri)
        {
            var cachedJwks = RetrieveFromCache(jwksUri);
            if (cachedJwks != null)
            {
                _logger.LogInformation("{JwksUri} - cache hit. Data: {CachedJwks}", jwksUri, cachedJwks);
                return cachedJwks;
            }

            return await RefreshJwks(jwksUri);
        }

        public async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> GetJwks(Uri jwksUri, string kid)
        {
            // Retrieve the jwks, which could be from cache.
            var jwks = await GetJwks(jwksUri);

            // Check the jwks to see if the given kid is included in the set.
            if (jwks.Keys.Any(k => k.Kid == kid))
            {
                _logger.LogInformation("Matching kid ({Kid}) was found in jwks", kid);
                return jwks;
            }

            // Not included, so refresh the jwks.
            _logger.LogInformation("Matching kid ({Kid}) was not found in jwks.  Refreshing jwks...", kid);
            return await RefreshJwks(jwksUri);
        }

        public async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> RefreshJwks(Uri jwksUri)
        {
            HttpResponseMessage httpResponse;
            try
            {
                _logger.LogInformation("Refreshing the jwks from {JwksUri}", jwksUri);
                httpResponse = await _httpClient.GetAsync(jwksUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred retrieving JWKS from {JwksUri}", jwksUri);
                throw new JwksException($"An error occurred retrieving JWKS from {jwksUri} - {ex.Message}");
            }

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("{JwksUri} returned 404.", jwksUri);
                throw new JwksException($"{jwksUri} returned 404.");
            }
            else if (!httpResponse.IsSuccessStatusCode)
            {
                var statusCode = httpResponse.StatusCode;
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError(
                    "{jwksUri} returned {statusCode} Content:\r\n{responseContent}",
                    jwksUri,
                    statusCode,
                    responseContent);
                throw new JwksException($"{jwksUri} returned {statusCode} Content:\r\n{responseContent}");
            }

            var jwks = await GetJwksFromResponse(jwksUri, httpResponse);
            AddToCache(jwksUri, jwks);
            return jwks;
        }

        private async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet?> GetJwksFromResponse(Uri jwksUri, HttpResponseMessage httpResponse)
        {
            try
            {
                return await httpResponse.Content.ReadAsJson<Microsoft.IdentityModel.Tokens.JsonWebKeySet>();
            }
            catch
            {
                _logger.LogError("No valid JWKS found from {jwksUri}", jwksUri);
                throw new JwksException($"No valid JWKS found from {jwksUri}");
            }
        }

        private void AddToCache(Uri jwksUri, Microsoft.IdentityModel.Tokens.JsonWebKeySet jwks)
        {
            _cache.Set(jwksUri.ToString(), jwks, absoluteExpiration: DateTimeOffset.Now.AddMinutes(_config.GetValue<int>("CacheExpiryMinutes", 5)));
        }

        private Microsoft.IdentityModel.Tokens.JsonWebKeySet? RetrieveFromCache(Uri jwksUri)
        {
            if (_cache.TryGetValue<Microsoft.IdentityModel.Tokens.JsonWebKeySet>(jwksUri.ToString(), out var jwks))
            {
                return jwks;
            }

            return null;
        }
    }
}
