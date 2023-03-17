using CdrAuthServer.Configuration;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Authorisation
{
    public class ScopeHandler : AuthorizationHandler<ScopeRequirement>
    {
        private readonly ILogger<ScopeHandler> _logger;
        private readonly ConfigurationOptions _configOptions;

        public ScopeHandler(
            ILogger<ScopeHandler> logger,
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configOptions = config.GetConfigurationOptions(httpContextAccessor.HttpContext);
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
        {
            // Check that authentication was successful before doing anything else
            if (context.User.Identity?.IsAuthenticated is not true)
            {
                return Task.CompletedTask;
            }

            // If user does not have the scope claim, get out of here
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

            if (userClaimScopes.Intersect(requiredScopes).Any())
            {
                _logger.LogInformation("Required scopes found {@requiredScopes}", requiredScopes);
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
