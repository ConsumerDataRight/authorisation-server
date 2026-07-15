using CdrAuthServer.API.Logger;
using CdrAuthServer.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Serilog;

/// <summary>
/// Extension functionality for configuring Application Insights.
/// </summary>
public static class ApplicationInsightsExtensions
{
    /// <summary>
    /// Conditionally enable Application Insights if any of the following Application Insights configuration values:
    /// <list type="bullet">
    /// <item><see cref="ApplicationInsightsKeys.ConnectionString">APPLICATIONINSIGHTS_CONNECTION_STRING</see></item>
    /// </list>
    /// </summary>
    /// <param name="loggerConfiguration">The existing logger configuration to which ApplicationInsights needs to be added.</param>
    /// <param name="telemetryConfiguration">The telemetry configuration.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The logger configuration with Application Insights sink configured (if applicable).</returns>
    public static LoggerConfiguration AddApplicationInsights(this LoggerConfiguration loggerConfiguration, TelemetryConfiguration? telemetryConfiguration, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>(ApplicationInsightsKeys.ConnectionString);

        if (connectionString is not null)
        {
            loggerConfiguration.WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces);
        }

        return loggerConfiguration;
    }

    /// <summary>
    /// Add application insights with register specific considerations.
    /// </summary>
    /// <param name="services">The services collection.</param>
    public static void AddCdrApplicationInsights(this IServiceCollection services)
    {
        services.AddSingleton<ITelemetryInitializer, CdrApplicationInitializer>();
        services.AddApplicationInsightsTelemetry();
    }
}
