using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class TokenResponse
    {
        [JsonProperty(ClaimNames.IdToken)]
        public string? IdToken { get; set; }

        [JsonProperty(ClaimNames.AccessToken)]
        public string? AccessToken { get; set; }

        [JsonProperty(ClaimNames.RefreshToken)]
        public string? RefreshToken { get; set; }

        [JsonProperty(ClaimNames.TokenType)]
        public string? TokenType { get; set; }

        [JsonProperty(ClaimNames.ExpiresIn)]
        public int ExpiresIn { get; set; }

        [JsonProperty(ClaimNames.CdrArrangementId)]
        public string? CdrArrangementId { get; set; }

        [JsonProperty(ClaimNames.Scope)]
        public string? Scope { get; set; }

        [JsonIgnore]
        public Error? Error { get; set; }
    }
}
