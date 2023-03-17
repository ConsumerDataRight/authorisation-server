using Microsoft.AspNetCore.Authorization;

namespace CdrAuthServer.Authorisation
{
    public class ScopeRequirement : IAuthorizationRequirement
    {
        public string Scope { get; }

        public ScopeRequirement(string scope)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }
    }
}
