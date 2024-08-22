using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Authorisation
{
    public class ScopeHandler : AuthorizationHandler<ScopeRequirement>
    {
        private readonly ILogger<ScopeHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly ConfigurationOptions _configOptions;

        public ScopeHandler(
            ILogger<ScopeHandler> logger,
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _config = config;
            _configOptions = _config.GetConfigurationOptions(_httpContextAccessor.HttpContext);
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
        {
            // Check that authentication was successful before doing anything else
            _logger.LogInformation("User is authenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
            if (context.User.Identity?.IsAuthenticated is not true)
            {
                return Task.CompletedTask;
            }

            // If user does not have the scope claim, get out of here
            _logger.LogInformation("Issuer: {Issuer}", _configOptions.Issuer);
            if (!context.User.HasClaim(c => c.Type == ClaimNames.Scope && c.Issuer == _configOptions.Issuer))
            {
                return Task.CompletedTask;
            }

            // Return the user claim scope
            var userClaimScopes = context.User.Claims
                .Where(c => c.Type == "scope")
                .Select(c => c.Value);

            // Succeed if the scope array contains the required scope
            // The space character is used to seperate the scopes as this is in line with CDS specifications.
            string[] requiredScopes = requirement.Scope.Split(' ');

            _logger.LogInformation("User scopes: {@UserClaimScopes}", userClaimScopes);
            _logger.LogInformation("Required scopes: {@RequiredScopes}", requiredScopes);

            if (userClaimScopes.Intersect(requiredScopes).Any())
            {
                _logger.LogInformation("Required scopes found {@RequiredScopes}", requiredScopes);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Required scopes not found in user scopes");
            }

            return Task.CompletedTask;
        }
    }
}
