namespace CdrAuthServer.Infrastructure.Authorisation
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class AuthorisationPolicy : Attribute
    {
        public AuthorisationPolicy(string name, string? scopeRequirement, bool hasMtlsRequirement, bool hasHolderOfKeyRequirement, bool hasAccessTokenRequirement)
        {
            Name = name;
            ScopeRequirement = scopeRequirement;
            HasMtlsRequirement = hasMtlsRequirement;
            HasHolderOfKeyRequirement = hasHolderOfKeyRequirement;
            HasAccessTokenRequirement = hasAccessTokenRequirement;
        }

        public string Name { get; private set; }
        public string? ScopeRequirement { get; private set; }
        public bool HasMtlsRequirement { get; private set; } //TODO: Currently not fully implemented in AuthServer. Implement in future to align with Mock Register
        public bool HasHolderOfKeyRequirement { get; private set; }
        public bool HasAccessTokenRequirement { get; private set; }
    }
}
