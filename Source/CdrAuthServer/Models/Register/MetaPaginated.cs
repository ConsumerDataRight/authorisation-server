namespace CdrAuthServer.Models.Register
{
    /// <summary>
    /// Paging metadata.
    /// </summary>
    /// <remarks><see href="https://consumerdatastandardsaustralia.github.io/standards/#cdr-participant-discovery-api_schemas_tocSmetapaginated"/>.</remarks>
    public class MetaPaginated : Meta
    {
        /// <summary>
        /// The total number of pages in the full set.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// The total number of records in the full set.
        /// </summary>
        public int TotalRecords { get; set; }
    }
}
