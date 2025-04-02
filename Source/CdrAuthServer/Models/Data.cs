using System.Text.Json.Serialization;

namespace CdrAuthServer.Models
{
    public class Data
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;
    }
}
