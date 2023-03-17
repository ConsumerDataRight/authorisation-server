using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;

#nullable enable

namespace CdrAuthServer.E2ETests
{
    public class TestFixture : IAsyncLifetime
    {
        virtual public Task InitializeAsync()
        {
            Debug.WriteLine($"{nameof(TestFixture)}.{nameof(InitializeAsync)}");

            // Only install Playwright if not running in container, since Dockerfile.e2e-tests already installed Playwright
            if (!BaseTest_v2.RUNNING_IN_CONTAINER)
            {
                // Ensure that Playwright has been fully installed.
                Microsoft.Playwright.Program.Main(new string[] { "install" });
                Microsoft.Playwright.Program.Main(new string[] { "install-deps" });
            }

            return Task.CompletedTask;
        }

        virtual public Task DisposeAsync()
        {
            Debug.WriteLine($"{nameof(TestFixture)}.{nameof(DisposeAsync)}");            

            return Task.CompletedTask;
        }       
    }
}
