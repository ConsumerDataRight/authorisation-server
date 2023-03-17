using CdrAuthServer.Models;

namespace CdrAuthServer.Extensions
{
    public static class SoftwareProductExtensions
    {
        public static bool IsActive(this SoftwareProduct softwareProduct)
        {
            return softwareProduct.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)
                && softwareProduct.BrandStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)
                && softwareProduct.LegalEntityStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetStatusDescription(this SoftwareProduct softwareProduct)
        {
            if (!softwareProduct.LegalEntityStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return softwareProduct.LegalEntityStatus;
            }

            if (!softwareProduct.BrandStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return softwareProduct.BrandStatus;
            }

            return softwareProduct.Status;
        }
    }
}
