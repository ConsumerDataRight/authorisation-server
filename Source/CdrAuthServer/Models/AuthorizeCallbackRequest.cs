using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class AuthorizeCallbackRequest : AuthorizeRequest
	{
		[JsonProperty(nameof(subject_id))]
		public string subject_id { get; set; } = string.Empty;

		[JsonProperty(nameof(account_ids))]
        public string account_ids { get; set; } = string.Empty;

        [JsonProperty(nameof(error_code))]
        public string error_code { get; set; } = string.Empty;
    }
}
