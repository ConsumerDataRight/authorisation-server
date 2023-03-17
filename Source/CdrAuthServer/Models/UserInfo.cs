using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class UserInfo
    {
        [JsonProperty("given_name")]
        public string GivenName { get; set; } = string.Empty;

        [JsonProperty("family_name")]
        public string FamilyName { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("aud")]
        public string Audience { get; set; } = string.Empty;

        [JsonProperty("iss")]
        public string Issuer { get; set; } = string.Empty;

        [JsonProperty("sub")]
        public string Subject { get; set; } = string.Empty;
    }
}
