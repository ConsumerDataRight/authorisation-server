using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class AuthorizationRequestObject
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonProperty("redirect_uri")]
        public string RedirectUri { get; set; } = string.Empty;

        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;

        [JsonProperty("nonce")]
        public string Nonce { get; set; } = string.Empty;

        [JsonProperty("response_type")]
        public string ResponseType { get; set; } = string.Empty;

        [JsonProperty("grant_type")]
        public string GrantType { get; set; } = string.Empty;

        [JsonProperty("response_mode")]
        public string ResponseMode { get; set; } = string.Empty;

        [JsonProperty("code_challenge")]
        public string CodeChallenge { get; set; } = string.Empty;

        [JsonProperty("code_challenge_method")]
        public string CodeChallengeMethod { get; set; } = string.Empty;

        [JsonProperty("claims")]
        public AuthorizeClaims Claims { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonProperty("scopes")]
        public IEnumerable<string> Scopes
        {
            get
            {
                if (string.IsNullOrEmpty(Scope))
                {
                    return Array.Empty<string>();
                }

                return Scope.Split(' ').Distinct().OrderBy(x => x);
            }
        }
    }
}
