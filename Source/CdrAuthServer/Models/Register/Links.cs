namespace CdrAuthServer.Models.Register
{
    /// <summary>
    /// Links related to the contextual API call.
    /// </summary>
    /// <remarks><see href="https://consumerdatastandardsaustralia.github.io/standards/#cdr-participant-discovery-api_schemas_tocSlinks"/>.</remarks>
    public class Links
    {
        /// <summary>
        /// Fully qualified link to the contextual API call.
        /// </summary>
        public Uri? Self { get; set; }
    }
}
