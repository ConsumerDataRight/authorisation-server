using CdrAuthServer.Infrastructure.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CdrAuthServer.SwaggerFilters
{
    public class SetupApiVersionParamsOperationFilter : IOperationFilter
    {
        private readonly CdrApiOptions _options;

        public SetupApiVersionParamsOperationFilter(IOptions<CdrApiOptions> options)
        {
            _options = options.Value;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionOption = _options.GetApiEndpointVersionOption($"/{context.ApiDescription.RelativePath}");

            foreach (var s in operation.Parameters.Where(o => o.Name == "x-v" || o.Name == "x-min-v"))
            {
                s.Required = false;

                if (versionOption != null && versionOption.IsXVHeaderMandatory && s.Name == "x-v")
                {
                    s.Required = true;
                }
            }
        }
    }
}
