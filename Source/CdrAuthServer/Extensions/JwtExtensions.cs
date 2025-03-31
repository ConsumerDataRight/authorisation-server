using System.IdentityModel.Tokens.Jwt;

namespace CdrAuthServer.Extensions
{
    public static class JwtExtensions
    {
        public static string? GetClaimValue(this JwtSecurityToken jwt, string claimType)
        {
            if (jwt == null || jwt.Payload == null || jwt.Payload.Claims == null || !jwt.Payload.Claims.Any())
            {
                return null;
            }

            if (jwt.Payload.TryGetValue(claimType, out var claimValue))
            {
                return claimValue == null ? null : claimValue.ToString();
            }

            return null;
        }

        public static bool IsExpired(this JwtSecurityToken jwt)
        {
            return jwt.ValidTo < DateTime.UtcNow;
        }
    }
}
