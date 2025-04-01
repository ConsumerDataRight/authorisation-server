using CdrAuthServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CdrAuthServer.Authorisation
{
    public class AccessTokenHandler : AuthorizationHandler<AccessTokenRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly ILogger<AccessTokenHandler> _logger;

        public AccessTokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration config, ILogger<AccessTokenHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _config = config;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessTokenRequirement requirement)
        {
            // Check that authentication was successful before doing anything else
            if (context.User.Identity?.IsAuthenticated is not true)
            {
                return;
            }

            // Check if the access token has been revoked.
            _logger.LogInformation("{AccessTokenHandler}.{HandleRequirementAsync} - Checking the access token...", nameof(AccessTokenHandler), nameof(HandleRequirementAsync));

            // Call the Mock Data Holder's idp to introspect the access token.
            var success = await CheckAccessToken();

            if (_httpContextAccessor.HttpContext != null)
            {
                if (success is true)
                {
                    _httpContextAccessor.HttpContext.Items["ValidAccessToken"] = true;
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogError("{AccessTokenHandler}.{HandleRequirementAsync} check failed.", nameof(AccessTokenHandler), nameof(HandleRequirementAsync));
                    _httpContextAccessor.HttpContext.Items["ValidAccessToken"] = false;
                    context.Fail(new AuthorizationFailureReason(this, "Access token is not valid"));
                }
            }
        }

        private async Task<bool?> CheckAccessToken()
        {
            // Get the Authorization header value.
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out StringValues authHeader) is not true)
            {
                _logger.LogError("Authorization header not found on HTTP request.");
                return false;
            }

            if (!authHeader.ToString().StartsWith("Bearer "))
            {
                _logger.LogError("Authorization header does not contain Bearer token.");
                return false;
            }

            // Introspect the access token.
            var accessToken = authHeader.ToString().Replace("Bearer ", string.Empty);
            var endpoint = _config["AccessTokenIntrospectionEndpoint"];

            var httpClient = new HttpClient(HttpHelper.CreateHttpClientHandler(_config));

            var formFields = new List<KeyValuePair<string, string>>
            {
                new("token", accessToken),
            };

            var response = await httpClient.PostAsync(endpoint, new FormUrlEncodedContent(formFields));

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<JObject>(body);
                return json?.GetValue("active")?.Value<bool>();
            }
            else
            {
                return false;
            }
        }
    }
}
