using CdrAuthServer;
using CdrAuthServer.API.Logger;
using CdrAuthServer.Authorisation;
using CdrAuthServer.Configuration;
using CdrAuthServer.Domain.Repositories;
using CdrAuthServer.Extensions;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.Infrastructure.Extensions;
using CdrAuthServer.Models;
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
using System.Text.RegularExpressions;
using static CdrAuthServer.Domain.Constants;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddScoped<ICertificateLoader, CertificateLoader>();
builder.Services.ConfigureWebServer(
    builder.Configuration,
    "Certificates:TlsInternalCertificate",
    httpPort: builder.Configuration.GetValue<int>("CdrAuthServer:HttpPort", 8080),
    httpsPort: builder.Configuration.GetValue<int>("CdrAuthServer:HttpsPort", 8001));

// Add logging provider.
var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
IdentityModelEventSource.ShowPII = true;

// Turn off default model validation so that it can be handled according to standards.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddHttpClient<IJwksService, JwksService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true
        };
        return handler;
    });

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
    metadataAddress = $"{validIssuers.First()}/.well-known/openid-configuration";
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
            JsonWebKeySet = await LoadJwks($"{metadataAddress}/jwks")
        };

        options.TokenValidationParameters = BuildTokenValidationParameters(options, validIssuers, validAudiences, clockSkew);

        // Ignore server certificate issues when retrieving OIDC configuration and JWKS.
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
        };
    });

builder.Services.AddAuthorization();

// Authorization
builder.Services.AddMvcCore().AddAuthorization(options =>
{
    options.AddPolicy(AuthorisationPolicy.Registration.ToString(), policy =>
    {
        policy.Requirements.Add(new ScopeRequirement(Scopes.Registration));
        policy.Requirements.Add(new HolderOfKeyRequirement());
    });
    options.AddPolicy(AuthorisationPolicy.UserInfo.ToString(), policy =>
    {
        policy.Requirements.Add(new HolderOfKeyRequirement());
    });
    options.AddPolicy(AuthorisationPolicy.GetCustomerBasic.ToString(), policy =>
    {
        policy.Requirements.Add(new ScopeRequirement(Scopes.ResourceApis.Common.CustomerBasicRead));
        policy.Requirements.Add(new HolderOfKeyRequirement());
        policy.Requirements.Add(new AccessTokenRequirement());
    });
    options.AddPolicy(AuthorisationPolicy.GetBankingAccounts.ToString(), policy =>
    {
        policy.Requirements.Add(new ScopeRequirement(Scopes.ResourceApis.Banking.AccountsBasicRead));
        policy.Requirements.Add(new HolderOfKeyRequirement());
        policy.Requirements.Add(new AccessTokenRequirement());
    });
    options.AddPolicy(AuthorisationPolicy.AdminMetadataUpdate.ToString(), policy =>
    {
        policy.Requirements.Add(new ScopeRequirement(Scopes.AdminMetadataUpdate));
        policy.Requirements.Add(new HolderOfKeyRequirement());
    });
});

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllOrigins",
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

var connectionString = builder.Configuration.GetConnectionString(DbConstants.ConnectionStrings.Default);
builder.Services.AddDbContext<CdrAuthServervDatabaseContext>(options => options.UseSqlServer(connectionString));

if (builder.Configuration.GetSection("SerilogRequestResponseLogger") != null)
{
    Log.Logger.Information("Adding request response logging middleware");
    builder.Services.AddRequestResponseLogging();
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services
    .AddControllers(options =>
    {
        // Requiring a specialised JWT formatter, as core uses JSON by default.
        options.InputFormatters.Clear();
        options.InputFormatters.Add(new JwtInputFormatter());
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
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
        var matches = Regex.Matches(context.Request.Path, basePathExpression, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeout:TimeSpan.FromMilliseconds(500));
        if (matches.Any())
        {
            var path = matches[0].Groups[0].Value;
            var remainder = matches[0].Groups[1].Value;
            context.Request.Path = $"/{remainder}";
            context.Request.PathBase = path.Replace(remainder, "").TrimEnd('/');
        }

        return next(context);
    });
}

app.UseRouting();
app.UseCors();

app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Unhandled excecptions.
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        // Try and retrieve the error from the ExceptionHandler middleware
        var exceptionDetails = context.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionDetails?.Error;
        var error = new CdsErrorList();
        error.Errors.Add(new CdsError() { Code = ErrorCodes.UnexpectedError, Title = "Unexpected Error Encountered", Detail = ex?.Message });
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

healthCheckSeedData = true;
healthCheckSeedDataMessage = "Seeding of data completed";

app.UseHealthChecks("/health", new HealthCheckOptions()
{
    ResponseWriter = CustomResponseWriter
});

app.Run();



static Task CustomResponseWriter(HttpContext context, HealthReport healthReport)
{
    context.Response.ContentType = "application/json";
    var result = JsonConvert.SerializeObject(new
    {
        status = healthReport.Entries.Select(e => new
        {
            key = e.Key,
            value = e.Value.Status.ToString(),
        })
    });
    return context.Response.WriteAsync(result);
}

static async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> LoadJwks(string jwksUri)
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (a, b, c, d) => true
    };
    var httpClient = new HttpClient(handler);
    var httpResponse = await httpClient.GetAsync(jwksUri);
    return await httpResponse.Content.ReadAsJson<Microsoft.IdentityModel.Tokens.JsonWebKeySet>();
}

void MigrateDatabase()
{
    var optionsBuilder = new DbContextOptionsBuilder<CdrAuthServervDatabaseContext>();
    // Run migrations if the DBO connection string is set.
    var migrationsConnectionString = builder.Configuration.GetConnectionString(DbConstants.ConnectionStrings.Migrations);
    if (!string.IsNullOrEmpty(migrationsConnectionString))
    {
        logger.Information("Found connection string for migrations dbo, migrating database");
        optionsBuilder.UseSqlServer(migrationsConnectionString);
        using var dbContext = new CdrAuthServervDatabaseContext(optionsBuilder.Options);
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

            foreach (var validIssuer in validationParameters.ValidIssuers)
            {
                if (issuer.StartsWith(validIssuer))
                {
                    return issuer;
                }
            }

            string errorMessage = $"IDX10205: Issuer validation failed. Issuer: '{issuer}'. Did not match: '{string.Join(',', validationParameters.ValidIssuers)}'.";
            throw new SecurityTokenInvalidIssuerException(errorMessage)
            {
                InvalidIssuer = issuer
            };
        },

        ValidateAudience = true,
        ValidAudiences = validAudiences,
        AudienceValidator = (IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters) =>
        {
            bool isValid = false;

            foreach (var audience in audiences)
            {
                if (validationParameters.ValidAudiences.Contains(audience, StringComparer.OrdinalIgnoreCase))
                {
                    isValid = true;
                    break;
                }

                foreach (var validAudience in validationParameters.ValidAudiences)
                {
                    if (audience.StartsWith(validAudience))
                    {
                        isValid = true;
                        break;
                    }
                }
            }

            if (!isValid)
            {
                string errorMessage = $"IDX10214: Audience validation failed. Audiences: '{string.Join(',', audiences)}'. Did not match: '{string.Join(',', validationParameters.ValidAudiences)}'.";
                throw new SecurityTokenInvalidAudienceException(errorMessage)
                {
                    InvalidAudience = string.Join(',', audiences)
                };
            }

            return isValid;
        },

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(clockSkew),

        RequireSignedTokens = true,
        IssuerSigningKeys = options.Configuration.JsonWebKeySet.Keys
    };
}
