using System.Net;

namespace CdrAuthServer.Infrastructure.Models
{
    public class Response
    {
        public Response()
        {
            this.Errors = new ErrorList();
        }

        public bool IsSuccessful 
        { 
            get
            {
                return ((int) this.StatusCode) < 400;
            }
        }

        public HttpStatusCode StatusCode { get; set; }

        public string? Message { get; set; }

        public ErrorList Errors { get; set; }
    }

    public class Response<T> : Response
    {
        public T? Data { get; set; }
    }

    public class Error
    {
        public Error()
        {
            this.Meta = new object();
        }

        public Error(string code, string title, string detail) : this()
        {
            this.Code = code;
            this.Title = title;
            this.Detail = detail;
        }

        /// <summary>
        /// Error code
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Error title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Error detail
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// Optional additional data for specific error types
        /// </summary>
        public object Meta { get; set; }
    }

    public class ErrorList
    {
        public List<Error> Errors { get; set; }

        public bool HasErrors()
        {
            return Errors != null && Errors.Any();
        }

        public ErrorList()
        {
            this.Errors = new List<Error>();
        }

        public ErrorList(Error error)
        {
            this.Errors = new List<Error>() { error };
        }

        public ErrorList(string errorCode, string errorTitle, string errorDetail)
        {
            var error = new Error(errorCode, errorTitle, errorDetail);
            this.Errors = new List<Error>() { error };
        }
    }
}
