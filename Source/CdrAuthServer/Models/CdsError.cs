using Newtonsoft.Json;

namespace CdrAuthServer.Models
{
    public class CdsError
    {
        public CdsError()
        {
            this.Meta = new object();
        }

        public CdsError(string code, string title, string detail) : this()
        {
            this.Code = code;
            this.Title = title;
            this.Detail = detail;
        }

        /// <summary>
        /// Error code
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Error title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Error detail
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Optional additional data for specific error types
        /// </summary>
        public object Meta { get; set; } = string.Empty;
    }
}
