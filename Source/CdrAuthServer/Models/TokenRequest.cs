using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class TokenRequest
    {
        [JsonProperty(ClaimNames.GrantType)]
        public string Grant_type { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.Code)]
        public string Code { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.CodeVerifier)]
        public string Code_verifier { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.RedirectUri)]
        public string Redirect_uri { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.ClientId)]
        public string Client_id { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.Scope)]
        public string Scope { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.RefreshToken)]
        public string Refresh_token { get; set; } = string.Empty;
    }
}
