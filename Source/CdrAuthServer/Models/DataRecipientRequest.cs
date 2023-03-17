using System.Text.Json.Serialization;

namespace CdrAuthServer.Models
{
    public class Data
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }
    }

    public class DataRecipientRequest
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}
