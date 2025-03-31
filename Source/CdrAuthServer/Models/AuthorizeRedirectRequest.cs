using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class AuthorizeRedirectRequest
    {
        [JsonProperty("authorize_request")]
        public AuthorizeRequest? AuthorizeRequest { get; set; } = null;

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; } = string.Empty;

        [JsonProperty("dh_brand_name")]
        public string DhBrandName { get; set; } = string.Empty;

        [JsonProperty("dh_brand_abn")]
        public string DhBrandAbn { get; set; } = string.Empty;

        [JsonProperty("dr_brand_name")]
        public string DrBrandName { get; set; } = string.Empty;

        [JsonProperty("customer_id")]
        public string CustomerId { get; set; } = string.Empty;

        [JsonProperty("otp")]
        public string Otp { get; set; } = string.Empty;

        [JsonProperty("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonProperty("sharing_duration")]
        public int? SharingDuration { get; set; }
    }
}
