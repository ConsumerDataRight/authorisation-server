using CdrAuthServer.Models;
using System.Globalization;

namespace CdrAuthServer.Extensions
{
    public static class RequestHeadersExtensions
    {
        private static int[] validVersions = new int[] { 1 };

        public static (bool isValid, CdsErrorList? error, int? statusCode) ValidateVersion(this IHeaderDictionary headers)
        {
            // Try and get x-v value from request header
            if (!headers.TryGetValue("x-v", out var x_v))
            {
                return MissingRequiredHeaderError("x-v");
            }

            // x-v must be a positive integer.
            if (!int.TryParse(x_v, out int xvVersion) || xvVersion < 1)
            {
                return InvalidVersionError("x-v");
            }

            // If requested version is 1, then just return.
            if (xvVersion == 1)
            {
                return (true, null, null);
            }

            // No matching version, so check if a x-min-v header has been provided.
            if (!headers.ContainsKey("x-min-v"))
            {
                // x-min-v has not been provided, so throw an unsupported version error.
                return UnsupportedVersionError(validVersions.Min(), validVersions.Max());
            }

            // Check if the x-min-v is a positive integer.
            var x_min_v = headers["x-min-v"];

            // x-min-v must be a positive integer.
            if (!int.TryParse(x_min_v, out int xvMinVersion) || xvMinVersion < 1)
            {
                // Raise an invalid error.
                return InvalidVersionError("x-min-v");
            }

            // If x-min-v is greater than x-v then ignore it.
            if (xvMinVersion > xvVersion)
            {
                xvMinVersion = xvVersion;
            }

            // Find the largest supported version between x-min-v and x-v.
            var supportedVersions = validVersions
                .Where(v => (v >= xvMinVersion && v <= xvVersion));

            // No supported versions were found.
            if (!supportedVersions.Any())
            {
                return UnsupportedVersionError(validVersions.Min(), validVersions.Max());
            }

            // Return the highest support version.
            return (true, null, null);
        }

        public static (bool isValid, CdsErrorList? error, int? statusCode) ValidateAuthDate(this IHeaderDictionary headers)
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

        private static (bool isValid, CdsErrorList? error, int? statusCode) MissingRequiredHeaderError(string headerName)
        {
            var cdsError = new CdsErrorList();
            cdsError.Errors.Add(new CdsError("urn:au-cds:error:cds-all:Header/Missing", "Missing Required Header", headerName));
            return (false, cdsError, 400);
        }

        private static (bool isValid, CdsErrorList? error, int? statusCode) InvalidHeaderError(string headerName)
        {
            var cdsError = new CdsErrorList();
            cdsError.Errors.Add(new CdsError("urn:au-cds:error:cds-all:Header/Invalid", "Invalid Header", headerName));
            return (false, cdsError, 400);
        }

        private static (bool isValid, CdsErrorList? error, int? statusCode) InvalidVersionError(string headerName)
        {
            var cdsError = new CdsErrorList();
            cdsError.Errors.Add(new CdsError("urn:au-cds:error:cds-all:Header/InvalidVersion", "Invalid Version", headerName));
            return (false, cdsError, 400);
        }

        private static (bool isValid, CdsErrorList? error, int? statusCode) UnsupportedVersionError(int minVersion, int maxVersion)
        {
            var cdsError = new CdsErrorList();
            cdsError.Errors.Add(new CdsError("urn:au-cds:error:cds-all:Header/UnsupportedVersion", "Unsupported Version", $"Min: {minVersion}. Max: {maxVersion}"));
            return (false, cdsError, 406);
        }
    }
}
