using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace CdrAuthServer.Models.Register
{
    /// <summary>
    /// Links related to the contextual API call and paging.
    /// </summary>
    /// <remarks><see href="https://consumerdatastandardsaustralia.github.io/standards/#cdr-participant-discovery-api_schemas_tocSlinkspaginated"/>.</remarks>
    public class LinksPaginated : Links
    {
        /// <summary>
        /// URI to the first page of this set. Mandatory if this response is not the first page.
        /// </summary>
        public Uri? First { get; set; }

        /// <summary>
        /// URI to the last page of this set. Mandatory if this response is not the last page.
        /// </summary>
        public Uri? Last { get; set; }

        /// <summary>
        /// URI to the next page of this set. Mandatory if this response is not the last page.
        /// </summary>
        public Uri? Next { get; set; }

        /// <summary>
        /// URI to the previous page of this set. Mandatory if this response is not the first page.
        /// </summary>
        [JsonPropertyName("prev")]
        public Uri? Previous { get; set; }
    }
}
