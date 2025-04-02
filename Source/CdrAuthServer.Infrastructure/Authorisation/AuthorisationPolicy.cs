namespace CdrAuthServer.Infrastructure.Authorisation
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
#pragma warning disable S3376 // Attribute, EventArgs, and Exception type names should end with the type being extended
    public class AuthorisationPolicy : Attribute
#pragma warning restore S3376 // Attribute, EventArgs, and Exception type names should end with the type being extended
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

#pragma warning disable S1135 // Track uses of "TODO" tags
        public bool HasMtlsRequirement { get; private set; } // TODO: Currently not fully implemented in AuthServer. Implement in future to align with Mock Register
#pragma warning restore S1135 // Track uses of "TODO" tags

        public bool HasHolderOfKeyRequirement { get; private set; }

        public bool HasAccessTokenRequirement { get; private set; }
    }
}
