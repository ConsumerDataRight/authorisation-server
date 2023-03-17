using CdrAuthServer.Infrastructure.Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Security;

namespace CdrAuthServer.Infrastructure.Extensions
{
    public static class WebServerExtensions
    {
        public static void ConfigureWebServer(
            this IServiceCollection services,
            IConfiguration configuration,
            string serverCertificateConfigurationKey,
            int httpsPort,
            int? httpPort = null,
            bool requireClientCertificate = false)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.ConfigureHttpsDefaults(async httpsOptions =>
                {
                    httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                    httpsOptions.ServerCertificate = await ((new CertificateLoader()).Load(configuration, serverCertificateConfigurationKey));
                    httpsOptions.ClientCertificateMode = requireClientCertificate ? ClientCertificateMode.RequireCertificate : ClientCertificateMode.NoCertificate;
                });

                options.ListenAnyIP(httpsPort, opts => opts.UseHttps());

                if (httpPort.HasValue)
                {
                    options.ListenAnyIP(httpPort.Value);
                }
            });
        }
    }
}
