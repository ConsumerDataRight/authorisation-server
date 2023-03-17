namespace CdrAuthServer.Repository.Entities
{
    public class SoftwareProduct
    {
        public string SoftwareProductId { get; set; } = String.Empty;

        public string SoftwareProductName { get; set; } = String.Empty;

        public string SoftwareProductDescription { get; set; } = String.Empty;

        public string LogoUri { get; set; } = String.Empty;

        public string Status { get; set; } = String.Empty;

        public string LegalEntityId { get; set; } = String.Empty;

        public string LegalEntityName { get; set; } = String.Empty;

        public string LegalEntityStatus { get; set; } = String.Empty;

        public string BrandId { get; set; } = String.Empty;

        public string BrandName { get; set; } = String.Empty;

        public string BrandStatus { get; set; } = String.Empty;
    }
}
