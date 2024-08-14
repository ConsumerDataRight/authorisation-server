using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Authorisation
{
    public class HolderOfKeyHandler : AuthorizationHandler<HolderOfKeyRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HolderOfKeyHandler> _logger;
        private readonly ConfigurationOptions _configOptions;

        public HolderOfKeyHandler(
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor, 
            ILogger<HolderOfKeyHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configOptions = config.GetConfigurationOptions(_httpContextAccessor.HttpContext);
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HolderOfKeyRequirement requirement)
        {
            // Check that authentication was successful before doing anything else
            if (context.User.Identity?.IsAuthenticated is not true)
            {
                return Task.CompletedTask;
            }

            //
            //  Check that the thumbprint of the client cert used for TLS MA is the same
            //  as the one expected by the cnf:x5t#S256 claim in the access token 
            //
            string? requestHeaderClientCertThumprint = null;
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(_configOptions.ClientCertificateThumbprintHttpHeaderName, out StringValues headerThumbprints) is true)
            {
                if (headerThumbprints.Count > 1)
                {
                    _logger.LogError("Multiple client certificate thumbprints found in request header");
                    return Task.CompletedTask;
                }

                requestHeaderClientCertThumprint = headerThumbprints[0];
            }

            if (string.IsNullOrWhiteSpace(requestHeaderClientCertThumprint))
            {
                _logger.LogError("thumbprint of the client cert is null or whitespace");
                return Task.CompletedTask;
            }

            string? accessTokenClientCertThumbprint = null;
            var cnfJson = context.User.FindFirst(ClaimNames.Confirmation)?.Value;
            if (!string.IsNullOrWhiteSpace(cnfJson))
            {
                var cnf = JObject.Parse(cnfJson);
                accessTokenClientCertThumbprint = cnf.Value<string>("x5t#S256");
            }

            if (string.IsNullOrWhiteSpace(accessTokenClientCertThumbprint))
            {
                _logger.LogError("client certificate thumbprint not found in access token");
                return Task.CompletedTask;
            }

            if (!accessTokenClientCertThumbprint.Equals(requestHeaderClientCertThumprint))
            {
                _logger.LogError("client certificate thumbprint does not match request and access token");
                return Task.CompletedTask;
            }

            // If we get this far all good
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
