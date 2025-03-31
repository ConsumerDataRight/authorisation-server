using System.Text.RegularExpressions;
using CdrAuthServer;
using CdrAuthServer.API.Logger;
using CdrAuthServer.Authorisation;
using CdrAuthServer.Configuration;
using CdrAuthServer.Domain.Models;
using CdrAuthServer.Domain.Repositories;
using CdrAuthServer.Extensions;
using CdrAuthServer.Helpers;
using CdrAuthServer.HttpPipeline;
using CdrAuthServer.Infrastructure.Authorisation;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.Infrastructure.Extensions;
using CdrAuthServer.Repository;
using CdrAuthServer.Repository.Infrastructure;
using CdrAuthServer.Services;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using Serilog.Settings.Configuration;
using static CdrAuthServer.Infrastructure.Constants;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSingleton<ICertificateLoader, CertificateLoader>();

await builder.Services.ConfigureWebServer(
    builder.Configuration,
    "Certificates:TlsInternalCertificate",
    httpPort: builder.Configuration.GetValue<int>("CdrAuthServer:HttpPort", 8080),
    httpsPort: builder.Configuration.GetValue<int>("CdrAuthServer:HttpsPort", 8001));

// Add logging provider.
ConfigureSerilog(builder.Configuration);
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
IdentityModelEventSource.ShowPII = true;

// Turn off default model validation so that it can be handled according to standards.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services
    .AddOptions<ConfigurationOptions>()
    .ValidateDataAnnotations()
    .Configure<IConfiguration>((options, config) => config.GetSection(ConfigurationOptions.ConfigurationSectionName).Bind(options));

builder.Services
    .AddOptions<CdrRegisterConfiguration>()
    .ValidateDataAnnotations()
    .Configure<IConfiguration>((options, config) => config
                                                    .GetSection(ConfigurationOptions.ConfigurationSectionName)
                                                    .GetSection(nameof(ConfigurationOptions.CdrRegister))
                                                    .Bind(options));

builder.Services.AddTransient<HttpLoggingDelegatingHandler>();

builder.Services.AddHttpClient<IJwksService, JwksService>()
    .ConfigurePrimaryHttpMessageHandler(s => HttpHelper.CreateHttpClientHandler(builder.Configuration))
    .AddHttpMessageHandler<HttpLoggingDelegatingHandler>();

builder.Services.AddHttpClient<IConsentRevocationService, ConsentRevocationService>()
    .ConfigurePrimaryHttpMessageHandler(s => HttpHelper.CreateHttpClientHandler(builder.Configuration))
    .AddHttpMessageHandler<HttpLoggingDelegatingHandler>();

builder.Services.AddHttpClient<IRegisterClientService, RegisterClientService>()
    .ConfigurePrimaryHttpMessageHandler(s => HttpHelper.CreateHttpClientHandler(builder.Configuration))
    .AddHttpMessageHandler<HttpLoggingDelegatingHandler>();

builder.Services.AddMemoryCache();

// Build up a list of valid issuers and audiences for the JWT bearer authorization.
var validIssuers = new List<string>();
var validAudiences = new List<string>() { "cds-au" };
var staticIssuer = builder.Configuration[Keys.Issuer];
var baseUri = builder.Configuration[Keys.BaseUri];
var basePath = builder.Configuration[Keys.BasePath];
var basePathExpression = builder.Configuration[Keys.BasePathExpression];
var metadataAddress = builder.Configuration[Keys.MetadataAddress];

// A static issuer has been set in config.
if (!string.IsNullOrEmpty(staticIssuer))
{
    validIssuers.Add(staticIssuer);
    validAudiences.Add(staticIssuer);
}
else
{
    // Build the issuer.
    validIssuers.Add($"{baseUri}{basePath}");
    validAudiences.Add($"{baseUri}{basePath}");
}

if (string.IsNullOrEmpty(metadataAddress))
{
    metadataAddress = $"{validIssuers[0]}/.well-known/openid-configuration";
}

// Add JWT Bearer Authorisation.
var clockSkew = builder.Configuration.GetValue<int>(Keys.ClockSkewSeconds, 0);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(async options =>
    {
        options.Configuration = new OpenIdConnectConfiguration()
        {
            JwksUri = $"{metadataAddress}/jwks",
            JsonWebKeySet = await LoadJwks($"{metadataAddress}/jwks", HttpHelper.CreateHttpClientHandler(builder.Configuration)),
        };

        options.TokenValidationParameters = BuildTokenValidationParameters(options, validIssuers, validAudiences, clockSkew);

        // Ignore server certificate issues when retrieving OIDC configuration and JWKS.
        options.BackchannelHttpHandler = HttpHelper.CreateHttpClientHandler(builder.Configuration);
    });

builder.Services.AddAuthorization();

// Authorization
builder.Services.AddMvcCore().AddAuthorization(options =>
{
    var allAuthPolicies = AuthorisationPolicies.GetAllPolicies();

    // Apply all listed policities from a single source of truth that is also used for self-documentation
    foreach (var pol in allAuthPolicies)
    {
        options.AddPolicy(pol.Name, policy =>
        {
            if (pol.ScopeRequirement != null && !pol.ScopeRequirement.IsNullOrEmpty())
            {
                policy.Requirements.Add(new ScopeRequirement(pol.ScopeRequirement));
            }

            if (pol.HasMtlsRequirement)
            {
                // policy.Requirements.Add(new MtlsRequirement()); //Currently not in CdrAuthServer but kept to later align with Mock Register
            }

            if (pol.HasHolderOfKeyRequirement)
            {
                policy.Requirements.Add(new HolderOfKeyRequirement());
            }

            if (pol.HasAccessTokenRequirement)
            {
                policy.Requirements.Add(new AccessTokenRequirement());
            }
        });
    }
});

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllOrigins",
        policy =>
        {
            policy.WithOrigins("*")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, ScopeHandler>();
builder.Services.AddScoped<IAuthorizationHandler, HolderOfKeyHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AccessTokenHandler>();
builder.Services.AddTransient<IClientAssertionValidator, ClientAssertionValidator>();
builder.Services.AddTransient<IJwtValidator, JwtValidator>();
builder.Services.AddTransient<IParValidator, ParValidator>();
builder.Services.AddScoped<IRequestObjectValidator, RequestObjectValidator>();
builder.Services.AddScoped<IClientRegistrationValidator, ClientRegistrationValidator>();
builder.Services.AddTransient<IAuthorizeRequestValidator, AuthorizeRequestValidator>();
builder.Services.AddTransient<ITokenRequestValidator, TokenRequestValidator>();
builder.Services.AddTransient<IClientService, ClientService>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddTransient<IGrantService, GrantService>();
builder.Services.AddTransient<ICustomerService, CustomerService>();
builder.Services.AddTransient<ICdrService, CdrService>();
builder.Services.AddTransient<IClientRepository, ClientRepository>();
builder.Services.AddTransient<ITokenRepository, TokenRepository>();
builder.Services.AddTransient<IGrantRepository, GrantRepository>();
builder.Services.AddTransient<ICustomerRepository, CustomerRepository>();
builder.Services.AddTransient<ICdrRepository, CdrRepository>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<ValidateMtlsAttribute>();
builder.Services.AddHttpClient<ValidateMtlsAttribute>();

var enableSwagger = builder.Configuration.GetValue<bool>(ConfigurationKeys.EnableSwagger);
if (enableSwagger)
{
    builder.Services.AddCdrSwaggerGen(
        opt =>
        {
            opt.SwaggerTitle = "Consumer Data Right (CDR) Participant Tooling - Mock Auth Server API";
            opt.IncludeAuthentication = true;
        },
        false);
}

var connectionString = builder.Configuration.GetConnectionString(DbConstants.ConnectionStrings.Default);
builder.Services.AddDbContext<CdrAuthServerDatabaseContext>(options => options.UseSqlServer(connectionString));

Log.Logger.Information("Adding request response logging middleware");
builder.Services.AddRequestResponseLogging();

// Add services to the container.
builder.Services
    .AddControllers(options =>
    {
        // Requiring a specialised JWT formatter, as core uses JSON by default.
        options.InputFormatters.Clear();
        options.InputFormatters.Add(new JwtInputFormatter());
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

bool healthCheckMigration = false;
string? healthCheckMigrationMessage = null;
bool healthCheckSeedData = false;
string? healthCheckSeedDataMessage = null;

builder.Services
    .AddHealthChecks()
    .AddCheck("migration", () => healthCheckMigration ? HealthCheckResult.Healthy(healthCheckMigrationMessage) : HealthCheckResult.Unhealthy(healthCheckMigrationMessage))
    .AddCheck("seed-data", () => healthCheckSeedData ? HealthCheckResult.Healthy(healthCheckSeedDataMessage) : HealthCheckResult.Unhealthy(healthCheckSeedDataMessage));

var app = builder.Build();
app.UseStaticFiles();

// A static base path can be set by the CdrAuthServer:BasePath app setting.
if (!string.IsNullOrEmpty(basePath))
{
    app.UsePathBase(basePath);
}

// A dynamic base path can be set by the CdrAuthServer:BasePathExpression app setting.
// This allows a regular expression to be set and matched rather than a static base path.
if (!string.IsNullOrEmpty(basePathExpression))
{
    app.Use((context, next) =>
    {
        var matches = Regex.Matches(context.Request.Path, basePathExpression, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeout: TimeSpan.FromMilliseconds(500));
        if (matches.Any())
        {
            var path = matches[0].Groups[0].Value;
            var remainder = matches[0].Groups[1].Value;
            context.Request.Path = $"/{remainder}";
            context.Request.PathBase = path.Replace(remainder, string.Empty).TrimEnd('/');
        }

        return next(context);
    });
}

app.UseRouting();
app.UseCors();

app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (enableSwagger)
{
    app.UseCdrSwagger("PT Auth Server");
}

app.MapControllers();

// Unhandled excecptions.
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        // Try and retrieve the error from the ExceptionHandler middleware
        var exceptionDetails = context.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionDetails?.Error;
        var error = new ResponseErrorList(CdrAuthServer.Domain.Constants.ErrorCodes.Cds.UnexpectedError, CdrAuthServer.Domain.Constants.ErrorTitles.UnexpectedError, ex?.Message ?? string.Empty);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(error));
    });
});

// Migrate database
healthCheckMigrationMessage = "Migration in progress";
healthCheckSeedDataMessage = "Seeding of data in progress";
MigrateDatabase();
healthCheckMigration = true;
healthCheckMigrationMessage = "Migration completed";

// Reconfigure Serilog with DB
ConfigureSerilog(builder.Configuration, true);

healthCheckSeedData = true;
healthCheckSeedDataMessage = "Seeding of data completed";

app.UseHealthChecks("/health", new HealthCheckOptions()
{
    ResponseWriter = CustomResponseWriter,
});

await app.RunAsync();

static void ConfigureSerilog(IConfiguration configuration, bool isDatabaseReady = false)
{
    var loggerConfiguration = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext();

    // If the database is ready, configure the SQL Server sink
    if (isDatabaseReady)
    {
        loggerConfiguration.ReadFrom.Configuration(configuration, new ConfigurationReaderOptions() { SectionName = "SerilogMSSqlServerWriteTo" });
    }

    Log.Logger = loggerConfiguration.CreateLogger();
}

static Task CustomResponseWriter(HttpContext context, HealthReport healthReport)
{
    context.Response.ContentType = "application/json";
    var result = JsonConvert.SerializeObject(new
    {
        status = healthReport.Entries.Select(e => new
        {
            key = e.Key,
            value = e.Value.Status.ToString(),
        }),
    });
    return context.Response.WriteAsync(result);
}

static async Task<JsonWebKeySet?> LoadJwks(string jwksUri, HttpMessageHandler httpMessageHandler)
{
    var httpClient = new HttpClient(httpMessageHandler);
    var httpResponse = await httpClient.GetAsync(jwksUri);
    return await httpResponse.Content.ReadAsJson<JsonWebKeySet>();
}

void MigrateDatabase()
{
    var optionsBuilder = new DbContextOptionsBuilder<CdrAuthServerDatabaseContext>();

    // Run migrations if the DBO connection string is set.
    var migrationsConnectionString = builder.Configuration.GetConnectionString(DbConstants.ConnectionStrings.Migrations);
    if (!string.IsNullOrEmpty(migrationsConnectionString))
    {
        Log.Logger.Information("Found connection string for migrations dbo, migrating database");
        optionsBuilder.UseSqlServer(migrationsConnectionString);
        using var dbContext = new CdrAuthServerDatabaseContext(optionsBuilder.Options);
        dbContext.Database.Migrate();
    }
}

static TokenValidationParameters BuildTokenValidationParameters(
    JwtBearerOptions options,
    IEnumerable<string> validIssuers,
    IEnumerable<string> validAudiences,
    int clockSkew)
{
    return new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidIssuers = validIssuers,
        IssuerValidator = (string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters) =>
        {
            if (validationParameters.ValidIssuers.Contains(issuer, StringComparer.OrdinalIgnoreCase))
            {
                return issuer;
            }

            var validIssuer = validationParameters.ValidIssuers.FirstOrDefault(v => v.Equals(issuer, StringComparison.OrdinalIgnoreCase) || issuer.StartsWith(v));
            if (validIssuer != null)
            {
                return issuer;
            }

            string errorMessage = $"IDX10205: Issuer validation failed. Issuer: '{issuer}'. Did not match: '{string.Join(',', validationParameters.ValidIssuers)}'.";
            throw new SecurityTokenInvalidIssuerException(errorMessage)
            {
                InvalidIssuer = issuer,
            };
        },

        ValidateAudience = true,
        ValidAudiences = validAudiences,
        AudienceValidator = (IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters) =>
                    {
                        var validAudiences = new HashSet<string>(validationParameters.ValidAudiences, StringComparer.OrdinalIgnoreCase);

                        bool isValid = audiences.Any(audience =>
                            validAudiences.Contains(audience) ||
                            validAudiences.Any(validAudience => audience.StartsWith(validAudience, StringComparison.OrdinalIgnoreCase)));

                        if (!isValid)
                        {
                            string errorMessage = $"IDX10214: Audience validation failed. Audiences: '{string.Join(',', audiences)}'. Did not match: '{string.Join(',', validationParameters.ValidAudiences)}'.";
                            throw new SecurityTokenInvalidAudienceException(errorMessage)
                            {
                                InvalidAudience = string.Join(',', audiences),
                            };
                        }

                        return isValid;
                    },

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(clockSkew),

        RequireSignedTokens = true,
        IssuerSigningKeys = options.Configuration!.JsonWebKeySet.Keys,
    };
}
