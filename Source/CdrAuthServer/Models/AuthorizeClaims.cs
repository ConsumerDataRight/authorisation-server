using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class AuthorizeClaims
    {
        [JsonProperty(PropertyName = "cdr_arrangement_id")]
        public string CdrArrangementId { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "sharing_duration")]
        public int? SharingDuration { get; set; }

        [JsonProperty(PropertyName = "id_token", Required = Required.Always)]
        public IdToken IdToken { get; set; } = null!;
    }
}
