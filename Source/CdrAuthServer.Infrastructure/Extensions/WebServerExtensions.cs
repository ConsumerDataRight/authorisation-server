using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CertificateLoader = CdrAuthServer.Infrastructure.Certificates.CertificateLoader;

namespace CdrAuthServer.Infrastructure.Extensions
{
    public static class WebServerExtensions
    {
        public static async Task ConfigureWebServer(
            this IServiceCollection services,
            IConfiguration configuration,
            string serverCertificateConfigurationKey,
            int httpsPort,
            int? httpPort = null,
            bool requireClientCertificate = false)
        {
            // Load the certificate asynchronously before configuring Kestrel
            var certificate = await new CertificateLoader().Load(configuration, serverCertificateConfigurationKey);

            services.Configure<KestrelServerOptions>(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                    httpsOptions.ServerCertificate = certificate;
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
