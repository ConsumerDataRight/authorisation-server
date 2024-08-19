using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.mTLS.Gateway.Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Diagnostics;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;
using Serilog;
using CdrAuthServer.mTLS.Gateway.Extensions;
using CdrAuthServer.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("gateway-config.json", false, true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.ConfigureWebServer(
    builder.Configuration, 
    "Certificates:MtlsServerCertificate", 
    httpsPort: builder.Configuration.GetValue<int>("CdrAuthServer:mtlsGateway:httpsPort", 8082), 
    requireClientCertificate: true);
//builder.Services.ConfigureCipherSuites();

// Add logging provider.
var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddScoped<ICertificateValidator, CertificateValidator>();
builder.Services.AddScoped<ICertificateLoader, CertificateLoader>();
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
 .AddCertificate(options =>
 {
     // Basic client certificate checks.
     options.AllowedCertificateTypes = CertificateTypes.All;
     options.ValidateCertificateUse = true;
     options.ValidateValidityPeriod = true;
     options.RevocationMode = X509RevocationMode.NoCheck;

     options.Events = new CertificateAuthenticationEvents
     {
         OnCertificateValidated = context =>
         {
             var certValidator = context.HttpContext.RequestServices.GetService<ICertificateValidator>();

             if (certValidator is null)
             {
                 return Task.CompletedTask;
             }

             certValidator.ValidateClientCertificate(context.ClientCertificate);
             context.Success();
             return Task.CompletedTask;
         },
         OnAuthenticationFailed = context =>
         {
             context.Fail("invalid client certificate");
             throw context.Exception;
         }
     };
 })
 // Adding an ICertificateValidationCache results in certificate auth caching the results.
 // The default implementation uses a memory cache.
 .AddCertificateCache();
builder.Services.AddAuthorization();
builder.Services.AddOcelot();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        // Try and retrieve the error from the ExceptionHandler middleware
        var exceptionDetails = context.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionDetails?.Error;

        if (ex is ClientCertificateException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
        }

        context.Response.ContentType = Text.Plain;
        await context.Response.WriteAsync($"An error occurred handling the request: {ex?.Message}");
    });
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var pipelineConfiguration = new OcelotPipelineConfiguration
{
    PreErrorResponderMiddleware = async (httpContext, next) =>
    {
        var clientCert = await httpContext.Connection.GetClientCertificateAsync();

        // The thumbprint and common name from the client certificate are extracted and added as headers for the downstream services.
        if (clientCert != null)
        {
            httpContext.Request.Headers[HttpHeaders.ClientCertificateThumbprint] = clientCert.Thumbprint;
            httpContext.Request.Headers[HttpHeaders.ClientCertificateCommonName] = clientCert.GetNameInfo(X509NameType.SimpleName, false);
        }

        // Send through the original host name to the backend service.
        httpContext.Request.Headers[HttpHeaders.ForwardedHost] = httpContext.Request.Host.ToString();

        await next.Invoke();
    }
};
app.UseOcelot(pipelineConfiguration).Wait();

app.Run();
