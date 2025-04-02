using CdrAuthServer.Domain.Models;
using System.Net;

namespace CdrAuthServer.Infrastructure.Models
{
    public class Response
    {
        public Response()
        {
            this.Errors = new ResponseErrorList(); // This might be right...as it may end up with Errors.Errors
        }

        public bool IsSuccessful
        {
            get
            {
                return ((int)this.StatusCode) < 400;
            }
        }

        public HttpStatusCode StatusCode { get; set; }

        public string? Message { get; set; }

        public ResponseErrorList Errors { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class Response<T> : Response
#pragma warning restore SA1402 // File may only contain a single type
    {
        public T? Data { get; set; }
    }
}
