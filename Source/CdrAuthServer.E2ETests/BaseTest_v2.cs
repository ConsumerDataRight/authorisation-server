#define TEST_DEBUG_MODE // Run Playwright in non-headless mode for debugging purposes (ie show a browser)

// In docker (Ubuntu container) Playwright will fail if running in non-headless mode, so we ensure TEST_DEBUG_MODE is undef'ed
#if !DEBUG
#undef TEST_DEBUG_MODE
#endif

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

#nullable enable

namespace CdrAuthServer.E2ETests
{
    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("E2ETests")]
    [TestCaseOrderer("CdrAuthServer.E2ETests.XUnit.Orderers.AlphabeticalOrderer", "CdrAuthServer.E2ETests")]
    [DisplayTestMethodName]
    public class BaseTest_v2
    {
        public const int FORM_REFRESH_DELAY = 3000;  // Amount of time to wait for form to refresh (milliseconds)

        static public bool RUNNING_IN_CONTAINER => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.ToUpper() == "TRUE";

        // Test settings.
        static public bool CREATE_MEDIA => Configuration.GetValue<bool>("CreateMedia", true);
        static public int TEST_TIMEOUT => Configuration.GetValue<int>("TestTimeout", 30000);

        // Media folder (for videos and screenshots)
        static public string MEDIAFOLDER => Configuration["MediaFolder"]
            ?? throw new Exception($"{nameof(MEDIAFOLDER)} - configuration setting not found");

        static private IConfigurationRoot? configuration;
        static protected IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .Build();
                }

                return configuration;
            }
        }

        private bool inArrange = false;
        protected delegate Task ArrangeDelegate(IPage page);

        protected async Task ArrangeAsync(
            ArrangeDelegate arrange,
            string? storageState = null)
        {
            if (inArrange)
                return;

            inArrange = true;
            try
            {
                await using var context = await PlaywrightHelper.NewBrowserContextAsync(createMedia: false, storageState: storageState);
                var page = await PlaywrightHelper.NewPageAsync(context);

                using (new AssertionScope())
                {
                    await arrange(page);
                }

                await context.CloseAsync();
            }
            finally
            {
                inArrange = false;
            }
        }

        private bool inCleanup = false;
        protected delegate Task CleanupDelegate(IPage page);
        protected async Task CleanupAsync(
            CleanupDelegate cleanup,
            string? storageState = null)
        {
            if (inCleanup)
                return;

            inCleanup = true;
            try
            {
                await using var context = await PlaywrightHelper.NewBrowserContextAsync(createMedia: false, storageState: storageState);
                var page = await PlaywrightHelper.NewPageAsync(context);

                using (new AssertionScope())
                {
                    await cleanup(page);
                }

                await context.CloseAsync();
            }
            finally
            {
                inArrange = false;
            }
        }

        protected delegate Task TestDelegate(IPage page);
        protected delegate Task ContextClosedDelegate();
        protected async Task TestAsync(string testName,
            TestDelegate testDelegate,
            ContextClosedDelegate? contextClosedDelegate = null,
            string? storageState = null)
        {
            await using var context = await PlaywrightHelper.NewBrowserContextAsync(storageState: storageState);
            var page = await PlaywrightHelper.NewPageAsync(context, testName);

            try
            {
                using (new AssertionScope())
                {
                    page.SetDefaultTimeout(TEST_TIMEOUT);

                    // Run test
                    await testDelegate(page);

                    // Wait to ensure final update is rendered
                    await Task.Delay(FORM_REFRESH_DELAY); // Give form time to refresh
                }
            }
            finally
            {
                // Take screenshot of final page state
                await PlaywrightHelper.ScreenshotAsync(page, testName);

                await context.Tracing.StopAsync(new()
                {
                    Path = $"{MEDIAFOLDER}/{testName}_trace.zip",
                });

                await context.CloseAsync();
                if (contextClosedDelegate != null)
                {
                    await contextClosedDelegate();
                }
            }
        }
    }
}