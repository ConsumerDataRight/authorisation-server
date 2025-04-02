using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CdrAuthServer.Models
{
    public class AuthorizeRequest
    {
        [JsonProperty(nameof(Request_uri))]
        public string Request_uri { get; set; } = string.Empty;

        [JsonProperty(nameof(Response_type))]
        public string Response_type { get; set; } = string.Empty;

        [JsonProperty(nameof(Response_mode))]
        public string Response_mode { get; set; } = string.Empty;

        [JsonProperty(nameof(Client_id))]
        public string Client_id { get; set; } = string.Empty;

        [JsonProperty(nameof(Redirect_uri))]
        public string Redirect_uri { get; set; } = string.Empty;

        [JsonProperty(nameof(Scope))]
        public string Scope { get; set; } = string.Empty;

        [JsonProperty(nameof(Nonce))]
        public string Nonce { get; set; } = string.Empty;
    }
}
