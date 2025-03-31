using CdrAuthServer.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CdrAuthServer.UnitTests.Helpers
{
    public class HttpHelperTests
    {
        private HttpClientHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _handler = HttpHelper.CreateHttpClientHandler(true);
        }

        [Test]
        [TestCase("mock-data-recipient.pfx", "#M0ckDataRecipient#", true, "Valid TLS certificate provisioned by trusted CA should return true")]
        [TestCase("mock-data-recipient-invalid.pfx", "#M0ckDataRecipient#", false, "Expired TLS certificate should throw exception")]
        [TestCase("jwks.pfx", "#M0ckDataRecipient#", false, "Self-signed TLS certificate should throw exception")]
        public async Task ServerCertificates_ValidationEnabled_ShouldValidateSslConnection(
            string certName, string certPassword, bool expected, string reason)
        {
            await using (var mockEndpoint = new MockEndpoint(
                "https://localhost:9990",
                Path.Combine(Directory.GetCurrentDirectory(), "Certificates", "MDR", certName),
                certPassword))
            {
                mockEndpoint.Start();
                var client = new HttpClient(_handler);
                if (expected)
                {
                    var result = await client.GetAsync("https://localhost:9990");
                    Assert.IsNotNull(result, reason);
                }
                else
                {
                    Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync("https://localhost:9990"), reason);
                }

                await mockEndpoint.Stop();
            }
        }
    }

    public partial class MockEndpoint : IAsyncDisposable
    {
        public MockEndpoint(string url, string certificatePath, string certificatePassword)
        {
            Url = url;
            CertificatePath = certificatePath;
            CertificatePassword = certificatePassword;
        }

        public string Url { get; init; }

        private int UrlPort => new Uri(Url).Port;

        public string CertificatePath { get; init; }

        public string CertificatePassword { get; init; }

        private IWebHost? _host;

        public void Start()
        {
            Log.Information("Calling {FUNCTION} in {ClassName}.", nameof(Start), nameof(MockEndpoint));

            _host = new WebHostBuilder()
                .UseKestrel(opts =>
        {
            opts.ListenAnyIP(
                UrlPort,
                opts => opts.UseHttps(new X509Certificate2(CertificatePath, CertificatePassword, X509KeyStorageFlags.Exportable)));
        })
               .UseStartup(_ => new MockEndpointStartup())
               .Build();

            _host.RunAsync();
        }

        public async Task Stop()
        {
            Log.Information("Calling {FUNCTION} in {ClassName}.", nameof(Stop), nameof(MockEndpoint));

            if (_host != null)
            {
                await _host.StopAsync();
            }
        }

        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            Log.Information("Calling {FUNCTION} in {ClassName}.", nameof(DisposeAsync), nameof(MockEndpoint));

            if (!_disposed)
            {
                await Stop();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        private class MockEndpointStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.UseHttpsRedirection();
                app.UseRouting();
            }

            public static void ConfigureServices(IServiceCollection services)
            {
                services.AddRouting();
            }
        }
    }
}
