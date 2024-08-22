using Microsoft.OpenApi.Extensions;

namespace CdrAuthServer.Infrastructure.Authorisation
{
    public static class AuthorisationPolicies
    {
        private static readonly Dictionary<AuthServerAuthorisationPolicyAttribute, AuthorisationPolicy> _policies = InitPolicies();

        public static AuthorisationPolicy GetPolicy(this AuthServerAuthorisationPolicyAttribute policy)
        {
            if (_policies.TryGetValue(policy, out var res))
            {
                return res;
            }

            throw new ArgumentOutOfRangeException($"Policy {policy} doesn't have any Authorisation Policy attribute");
        }

        public static List<AuthorisationPolicy> GetAllPolicies()
        {
            return _policies.Select(p => p.Value).ToList();
        }

        private static Dictionary<AuthServerAuthorisationPolicyAttribute, AuthorisationPolicy> InitPolicies()
        {
            var result = new Dictionary<AuthServerAuthorisationPolicyAttribute, AuthorisationPolicy>();
            foreach (AuthServerAuthorisationPolicyAttribute i in Enum.GetValues(typeof(AuthServerAuthorisationPolicyAttribute)))
            {
                var attr = i.GetAttributeOfType<AuthorisationPolicy>();

                if (attr != null)
                {
                    result.Add(i, new AuthorisationPolicy(attr.Name, attr.ScopeRequirement, attr.HasMtlsRequirement, attr.HasHolderOfKeyRequirement, attr.HasAccessTokenRequirement));
                }
            }

            return result;
        }
    }
}
