using System.Text.Json.Serialization;

namespace CdrAuthServer.Models
{
    public class DataRecipientRequest
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}
