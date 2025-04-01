using System.Globalization;
using CdrAuthServer.Domain.Models;

namespace CdrAuthServer.Extensions
{
    public static class RequestHeadersExtensions
    {
        public static (bool IsValid, ResponseErrorList? Error, int? StatusCode) ValidateAuthDate(this IHeaderDictionary headers)
        {
            // Get x-fapi-auth-date from request header
            var authDateValue = headers["x-fapi-auth-date"];
            if (authDateValue.Count == 0)
            {
                return MissingRequiredHeaderError("x-fapi-auth-date");
            }

            if (!DateTime.TryParseExact(authDateValue, CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern, CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None, out _))
            {
                return InvalidHeaderError("x-fapi-auth-date");
            }

            return (true, null, null);
        }

        private static (bool IsValid, ResponseErrorList? Error, int? StatusCode) MissingRequiredHeaderError(string headerName)
        {
            var errors = new ResponseErrorList().AddMissingRequiredHeader(headerName);
            return (false, errors, 400);
        }

        private static (bool IsValid, ResponseErrorList? Error, int? StatusCode) InvalidHeaderError(string headerName)
        {
            var errors = new ResponseErrorList().AddInvalidHeader(headerName);
            return (false, errors, 400);
        }
    }
}
