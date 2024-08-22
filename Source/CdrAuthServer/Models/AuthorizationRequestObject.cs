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

    public class AuthorizeClaims
    {
        [JsonProperty(PropertyName = "cdr_arrangement_id")]
        public string CdrArrangementId { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "sharing_duration")]
        public int? SharingDuration { get; set; }

        [JsonProperty(PropertyName = "id_token", Required = Required.Always)]
        public IdToken IdToken { get; set; }
    }

    public class IdToken
    {
        [JsonProperty(PropertyName = "acr", Required = Required.Always)]
        public Acr Acr { get; set; }
    }

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
