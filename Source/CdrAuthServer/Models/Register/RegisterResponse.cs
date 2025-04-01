namespace CdrAuthServer.Models.Register
{
    /// <summary>
    /// Represents a response from the CDR Register (without pagination).
    /// </summary>
    /// <typeparam name="TData">The data type to include in the response.</typeparam>
    public class RegisterResponse<TData>
        where TData : class, new()
    {
        /// <summary>
        /// The response data for the query.
        /// </summary>
        public IEnumerable<TData> Data { get; set; } = [];

        /// <summary>
        /// Links.
        /// </summary>
        public Links Links { get; set; } = new();

        /// <summary>
        /// Metadata.
        /// </summary>
        public Meta Meta { get; set; } = new();
    }
}
