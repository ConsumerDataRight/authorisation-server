using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class Acr
    {
        [JsonProperty(PropertyName = "essential")]
        public bool Essential { get; set; }

        [JsonProperty(PropertyName = "values")]
        public string[] Values { get; set; } = [];

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; } = string.Empty;
    }
}
