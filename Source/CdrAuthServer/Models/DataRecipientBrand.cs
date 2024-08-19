using System.Text.Json.Serialization;

namespace CdrAuthServer.Models.Json
{
    public class DataRecipientBrand
    {
        [JsonPropertyName("dataRecipientBrandId")]
        public string DataRecipientBrandId { get; set; } = string.Empty;

        [JsonPropertyName("brandName")]
        public string BrandName { get; set; } = string.Empty;

        [JsonPropertyName("softwareProducts")]
        public List<SoftwareProduct> SoftwareProducts { get; set; } = [];

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;             
    }

    public class LegalEntity
    {
        [JsonPropertyName("legalEntityId")]
        public string LegalEntityId { get; set; } = string.Empty ;

        [JsonPropertyName("legalEntityName")]
        public string LegalEntityName { get; set; } = string.Empty;

        [JsonPropertyName("dataRecipientBrands")]
        public List<DataRecipientBrand> DataRecipientBrands { get; set; } = [];

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
