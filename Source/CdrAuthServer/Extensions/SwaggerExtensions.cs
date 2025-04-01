using CdrAuthServer.Infrastructure.Configuration;
using CdrAuthServer.Infrastructure.Models;
using CdrAuthServer.SwaggerFilters;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CdrAuthServer.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddCdrSwaggerGen(this IServiceCollection services, Action<CdrSwaggerOptions> configureRegisterSwaggerOptions, bool isVersioned = true)
        {
            var options = new CdrSwaggerOptions();
            configureRegisterSwaggerOptions(options);

            services.Configure(configureRegisterSwaggerOptions);

            if (isVersioned)
            {
                services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

                // Required for our Swagger setup to work when endpoints have been versioned
                services.AddVersionedApiExplorer(opt =>
                {
                    opt.GroupNameFormat = options.VersionedApiGroupNameFormat;
                });
            }
            else
            {
                services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureUnversionedSwaggerOptions>();
            }

            services.AddSwaggerGen(c =>
            {
                // swagger comments from project xml documentation files
                var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                xmlFiles.ForEach(fileName => c.IncludeXmlComments(fileName));
                c.EnableAnnotations(); // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/README.md#enrich-parameter-metadata

                c.DocumentFilter<CustomDocumentFilter>();
                c.ParameterFilter<CustomParameterFilter>();
                c.SchemaFilter<PropertyAlphabeticalOrderFilter>();
                c.OperationFilter<SetupApiVersionParamsOperationFilter>();
                c.OperationFilter<AuthorizationOperationFilter>();

                if (options.IncludeAuthentication)
                {
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Please enter into field the word 'Bearer' following by space and JWT.",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Scheme = "bearer",
                        Type = SecuritySchemeType.ApiKey, // SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                            },
                            new List<string>()
                        },
                    });
                }
            });

            services.AddSwaggerGenNewtonsoftSupport();

            return services;
        }
    }
}
