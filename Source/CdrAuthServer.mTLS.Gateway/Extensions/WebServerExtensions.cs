using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace CdrAuthServer.mTLS.Gateway.Extensions
{
    public static class WebServerExtensions
    {
        public static void ConfigureCipherSuites(
            this IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                });
            });
        }
    }
}
