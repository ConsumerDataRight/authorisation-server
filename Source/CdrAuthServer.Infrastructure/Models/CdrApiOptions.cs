using Microsoft.AspNetCore.Http;

namespace CdrAuthServer.Infrastructure.Models
{
    public class CdrApiOptions
    {
        private static readonly List<CdrApiEndpointVersionOptions> _supportedApiVersions = new List<CdrApiEndpointVersionOptions>
            {
                //(?i) is to make the regex case insensitive
                //(%7B)* and (%7D)* represent the possibility of a { and } in the path, to cover the value '{industry}' for OAS generation purposes

                //Currently everything is unversioned or v1 with optional x-v, so we don't need to list anything
            };

        public List<CdrApiEndpointVersionOptions> EndpointVersionOptions { get; } = _supportedApiVersions;

        public string DefaultVersion { get; set; } = "1";

        public CdrApiEndpointVersionOptions? GetApiEndpointVersionOption(PathString path)
        {
            foreach (var supportedApi in EndpointVersionOptions.OrderByDescending(v => v.Path.Length))
            {
                var regEx = new System.Text.RegularExpressions.Regex(supportedApi.Path);
                if (regEx.IsMatch(path))
                {
                    return supportedApi;
                }
            }

            return null;
        }
    }
}
