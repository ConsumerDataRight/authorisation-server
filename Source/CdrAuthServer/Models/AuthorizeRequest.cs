using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CdrAuthServer.Models
{
    public class AuthorizeRequest
    {
        [JsonProperty(nameof(request_uri))]
        public string request_uri { get; set; } = string.Empty;

        [JsonProperty(nameof(response_type))]
        public string response_type { get; set; } = string.Empty;

        [JsonProperty(nameof(response_mode))]
        public string response_mode { get; set; } = string.Empty;

        [JsonProperty(nameof(client_id))]
        public string client_id { get; set; } = string.Empty;

        [JsonProperty(nameof(redirect_uri))]
        public string redirect_uri { get; set; } = string.Empty;

        [JsonProperty(nameof(scope))]
        public string scope { get; set; } = string.Empty;

        [JsonProperty(nameof(nonce))]
        public string nonce { get; set; } = string.Empty;
    }
}