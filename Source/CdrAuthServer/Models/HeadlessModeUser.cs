using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class HeadlessModeUser
    {
        [JsonProperty("given_name")]
        public string GivenName { get; } = "Kamilla";

        [JsonProperty("family_name")]
        public string FamilyName { get; } = "Smith";

        [JsonProperty("name")]
        public string Name { get; } = "Kamilla Smith";

        [JsonProperty("sub")]
        public string Subject { get; } = "ksmith";

        [JsonProperty("accounts")]
        public string[] Accounts { get; } = { "123456", "987654" };
    }
}
