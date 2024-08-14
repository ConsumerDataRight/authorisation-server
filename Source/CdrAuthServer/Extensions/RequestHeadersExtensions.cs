using CdrAuthServer.Domain.Models;
using CdrAuthServer.Infrastructure.Models;
using CdrAuthServer.Models;
using System.Globalization;

namespace CdrAuthServer.Extensions
{
    public static class RequestHeadersExtensions
    {
        public static (bool isValid, ResponseErrorList? error, int? statusCode) ValidateAuthDate(this IHeaderDictionary headers)
        {
            // Get x-fapi-auth-date from request header
            var authDateValue = headers["x-fapi-auth-date"];
            if (authDateValue.Count == 0)
            {
                return MissingRequiredHeaderError("x-fapi-auth-date");
            }

            if (authDateValue.Count > 0 &&
                !DateTime.TryParseExact(authDateValue, CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern, CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None, out DateTime authDate))
            {
                return InvalidHeaderError("x-fapi-auth-date");
            }

            return (true, null, null);
        }

        private static (bool isValid, ResponseErrorList? error, int? statusCode) MissingRequiredHeaderError(string headerName)
        {
            var errors = new ResponseErrorList().AddMissingRequiredHeader(headerName);
            return (false, errors, 400);
        }

        private static (bool isValid, ResponseErrorList? error, int? statusCode) InvalidHeaderError(string headerName)
        {
            var errors = new ResponseErrorList().AddInvalidHeader(headerName);
            return (false, errors, 400);
        }
    }
}
