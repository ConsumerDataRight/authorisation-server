using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class AuthorizeCallbackRequest : AuthorizeRequest
    {
        [JsonProperty(nameof(Subject_id))]
        public string Subject_id { get; set; } = string.Empty;

        [JsonProperty(nameof(Account_ids))]
        public string Account_ids { get; set; } = string.Empty;

        [JsonProperty(nameof(Error_code))]
        public string Error_code { get; set; } = string.Empty;
    }
}
