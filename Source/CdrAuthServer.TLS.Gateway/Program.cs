using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Certificates;
using CdrAuthServer.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using static System.Net.Mime.MediaTypeNames;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("gateway-config.json", false, true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddScoped<ICertificateLoader, CertificateLoader>();
builder.Services.ConfigureWebServer(
    builder.Configuration, 
    "Certificates:TlsServerCertificate", 
    httpsPort: builder.Configuration.GetValue<int>("CdrAuthServer:tlsGateway:httpsPort", 8081),
    requireClientCertificate: false);
builder.Services.AddOcelot();

// Add logging provider.
var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        // Try and retrieve the error from the ExceptionHandler middleware
        var exceptionDetails = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetService<ILogger>();
        var ex = exceptionDetails?.Error;
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        context.Response.ContentType = Text.Plain;

        logger?.Error("Caught exception with error: {ex}", ex);
        await context.Response.WriteAsync($"An error occurred handling the request: {ex?.Message}");
    });
});

app.UseHttpsRedirection();

var pipelineConfiguration = new OcelotPipelineConfiguration
{
    PreErrorResponderMiddleware = async (httpContext, next) =>
    {
        // Send through the original host name to the backend service.
        httpContext.Request.Headers[HttpHeaders.ForwardedHost] = httpContext.Request.Host.ToString();
        await next.Invoke();
    }
};
app.UseOcelot(pipelineConfiguration).Wait();

app.Run();
