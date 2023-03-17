namespace CdrAuthServer.Models
{
    public class SoftwareProduct
    {
        public string LegalEntityId { get; set; } = string.Empty;
        public string LegalEntityName { get; set; } = string.Empty;
        public string LegalEntityStatus { get; set; } = string.Empty;
        public string BrandId { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string BrandStatus { get; set; } = string.Empty;
        public string SoftwareProductId { get; set; } = string.Empty;
        public string SoftwareProductName { get; set; } = string.Empty;
        public string SoftwareProductDescription { get; set; } = string.Empty;
        public string LogoUri { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
