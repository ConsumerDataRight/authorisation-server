using Newtonsoft.Json;

namespace CdrAuthServer.Infrastructure.Models
{
#pragma warning disable SA1649 // File name should match first type name
    public class Token
#pragma warning restore SA1649 // File name should match first type name
    {
        [JsonProperty("id_token")]
        public string? IdToken { get; set; }

        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }

        [JsonProperty("cdr_arrangement_id")]
        public string? CdrArrangementId { get; set; }
    }
}
