using CdrAuthServer.IdPermanence;
using CdrAuthServer.Infrastructure;
using CdrAuthServer.Models;
using System.Security.Claims;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetClientId(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null || claimsPrincipal.Identity == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                return null;
            }

            return claimsPrincipal.Claims.GetClaimValue(ClaimNames.ClientId);
        }

        public static string? GetIssuer(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null || claimsPrincipal.Identity == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                return null;
            }

            return claimsPrincipal.Claims.GetClaimValue(ClaimNames.Issuer);
        }

        public static string? GetSubject(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null || claimsPrincipal.Identity == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                return null;
            }

            var sub = claimsPrincipal.Claims.GetClaimValue(ClaimNames.Subject);
            if (!string.IsNullOrEmpty(sub))
            {
                return sub;
            }

            return claimsPrincipal.Claims.GetClaimValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        }

        public static string? DecryptSub(this string subject, Client client, IConfiguration config)
        {
            var idPermanenceManager = new IdPermanenceManager(config);
            var param = new SubPermanenceParameters()
            {
                SoftwareProductId = client.SoftwareId,
                SectorIdentifierUri = client.SectorIdentifierUri ?? ""
            };

            return idPermanenceManager.DecryptSub(subject, param);
        }
    }
}
