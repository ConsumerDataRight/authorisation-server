using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class IdToken
    {
        [JsonProperty(PropertyName = "acr", Required = Required.Always)]
        public Acr? Acr { get; set; }
    }
}
