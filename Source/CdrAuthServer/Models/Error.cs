using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class Error
    {

        [JsonProperty("error")]
        public string Code { get; set; }

        [JsonProperty("error_description")]
        public string? Description { get; set; }

        public Error(string code, string? description = null)
        {
            this.Code = code;
            this.Description = description;
        }
    }
}
