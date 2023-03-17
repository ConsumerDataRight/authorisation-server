using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class TokenRequest
    {
        [JsonProperty(ClaimNames.GrantType)]
        public string grant_type { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.Code)]
        public string code { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.CodeVerifier)]
        public string code_verifier { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.RedirectUri)]
        public string redirect_uri { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.ClientId)]
        public string client_id { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.Scope)]
        public string scope { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.RefreshToken)]
        public string refresh_token { get; set; } = string.Empty;
    }
}
