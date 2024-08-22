using CdrAuthServer.Models;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Extensions
{
    public static class SoftwareProductExtensions
    {
        public static bool IsActive(this SoftwareProduct softwareProduct)
        {
            return softwareProduct.Status.Equals(EntityStatus.Active, StringComparison.OrdinalIgnoreCase)
                && softwareProduct.BrandStatus.Equals(EntityStatus.Active, StringComparison.OrdinalIgnoreCase)
                && softwareProduct.LegalEntityStatus.Equals(EntityStatus.Active, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetStatusDescription(this SoftwareProduct softwareProduct)
        {
            if (!softwareProduct.LegalEntityStatus.Equals(EntityStatus.Active, StringComparison.OrdinalIgnoreCase))
            {
                return softwareProduct.LegalEntityStatus;
            }

            if (!softwareProduct.BrandStatus.Equals(EntityStatus.Active, StringComparison.OrdinalIgnoreCase))
            {
                return softwareProduct.BrandStatus;
            }

            return softwareProduct.Status;
        }
    }
}
