using System.ComponentModel.DataAnnotations;
using CdrAuthServer.Domain.Extensions;

namespace CdrAuthServer.Domain.Models
{
    public class CdsError
    {
        public CdsError(string code, string title, string detail)
        {
            Code = code;
            Title = title;
            Detail = detail;
        }

        public CdsError(string code, string title, string detail, string metaUrn)
        {
            Code = code;
            Title = title;
            Detail = detail;
            Meta = metaUrn.IsNullOrWhiteSpace() ? null : new MetaError(metaUrn);
        }

        /// <summary>
        /// Error code
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// Error title
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Error detail
        /// </summary>
        [Required]
        public string Detail { get; set; }

        /// <summary>
        /// Optional additional data for specific error types
        /// </summary>
        public MetaError? Meta { get; set; }
    }
}
