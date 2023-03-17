using System.Security.Claims;

namespace CdrAuthServer.Extensions
{
    public static class ClaimsExtensions
    {
        public static string? GetClaimValue(this IEnumerable<Claim> claims, string claimName)
        {
            var claim = claims.FirstOrDefault(c => c.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase));
            return claim != null ? claim.Value : null;
        }
    }
}
