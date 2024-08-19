using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class PushedAuthorizationCreatedResponse
    {
        [JsonProperty("request_uri")]
        public string? RequestUri { get; set; }

        [JsonProperty("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}
