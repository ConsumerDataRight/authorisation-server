using System.Text.Json.Serialization;

namespace CdrAuthServer.Models.Json
{
    public class LegalEntity
    {
        [JsonPropertyName("legalEntityId")]
        public string LegalEntityId { get; set; } = string.Empty;

        [JsonPropertyName("legalEntityName")]
        public string LegalEntityName { get; set; } = string.Empty;

        [JsonPropertyName("dataRecipientBrands")]
        public List<DataRecipientBrand> DataRecipientBrands { get; set; } = [];

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
