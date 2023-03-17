#nullable disable // FIXME - MJS - fix nullable warnings

namespace CdrAuthServer.IntegrationTests.Models
{
    /// <summary>
    /// Access token
    /// </summary>
    public class AccessToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }
}
