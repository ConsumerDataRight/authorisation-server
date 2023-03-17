using System.Text.Json.Serialization;

namespace CdrAuthServer.Models.Json
{
    public class DataRecipientBrand
    {
        [JsonPropertyName("dataRecipientBrandId")]
        public string DataRecipientBrandId { get; set; }

        [JsonPropertyName("brandName")]
        public string BrandName { get; set; }

        [JsonPropertyName("softwareProducts")]
        public List<SoftwareProduct> SoftwareProducts { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }                
    }

    public class LegalEntity
    {
        [JsonPropertyName("legalEntityId")]
        public string LegalEntityId { get; set; }

        [JsonPropertyName("legalEntityName")]
        public string LegalEntityName { get; set; }

        [JsonPropertyName("dataRecipientBrands")]
        public List<DataRecipientBrand> DataRecipientBrands { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}
