using CdrAuthServer.Infrastructure.Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Security;

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

                    // On non-Windows platform the CipherSuitesPolicy can be set.
                    if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        httpsOptions.OnAuthenticate = (context, sslOptions) =>
                        {
                            // Set the cipher suites dictated by the CDS.
                            //sslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(
                            //    new[] {
                            //        TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
                            //        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                            //        TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
                            //        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
                            //    });
                        };
                    }
                });
            });
        }
    }
}
