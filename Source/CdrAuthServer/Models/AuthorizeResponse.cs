using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Models
{
    public class AuthorizeResponse
    {
        [JsonProperty(ClaimNames.Code)]
        public string Code { get; set; } = string.Empty;

        [JsonProperty(ClaimNames.State)]
        public string State { get; set; } = string.Empty;
    }
}
