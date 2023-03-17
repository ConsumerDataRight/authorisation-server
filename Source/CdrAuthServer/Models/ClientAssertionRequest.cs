using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class ClientAssertionRequest
    {
        [JsonProperty("grant_type")]
        public string? GrantType { get; set; }

        [JsonProperty("client_id")]
        public string? ClientId { get; set; }

        [JsonProperty("client_assertion_type")]
        public string? ClientAssertionType { get; set; }

        [JsonProperty("client_assertion")]
        public string? ClientAssertion { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }
    }
}
