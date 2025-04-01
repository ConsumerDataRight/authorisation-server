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
}
