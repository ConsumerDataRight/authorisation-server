using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class Introspection
    {
        [JsonProperty(ClaimNames.CdrArrangementId)]
        public string CdrArrangementId { get; set; }

        [JsonProperty(ClaimNames.Scope)]
        public string Scope { get; set; }

        [JsonProperty(ClaimNames.Expiry)]
        public int? Expiry { get; set; }

        [JsonProperty(ClaimNames.Active)]
        public bool IsActive { get; set; }
    }
}
