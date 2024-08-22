using CdrAuthServer.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CdrAuthServer.Infrastructure.Configuration
{
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly CdrSwaggerOptions _options;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IOptions<CdrSwaggerOptions> options)
        {
            _provider = provider;
            _options = options.Value;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                  description.GroupName,
                  new Microsoft.OpenApi.Models.OpenApiInfo()
                  {
                      Title = _options.SwaggerTitle,
                      Version = description.ApiVersion.ToString(),
                  });
                options.UseInlineDefinitionsForEnums();
            }
        }
    }
}
