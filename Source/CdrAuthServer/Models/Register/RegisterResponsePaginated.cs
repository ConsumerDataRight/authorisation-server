namespace CdrAuthServer.Models.Register
{
    /// <summary>
    /// Represents a response from the CDR register (with pagination).
    /// </summary>
    /// <typeparam name="TData">The data type to include in the response.</typeparam>
    public class RegisterResponsePaginated<TData>
        where TData : class, new()
    {
        /// <summary>
        /// The response data for the query.
        /// </summary>
        public IEnumerable<TData> Data { get; set; } = [];

        /// <summary>
        /// Paging Links.
        /// </summary>
        public LinksPaginated Links { get; set; } = new();

        /// <summary>
        /// Paging Metadata.
        /// </summary>
        public MetaPaginated Meta { get; set; } = new();
    }
}
