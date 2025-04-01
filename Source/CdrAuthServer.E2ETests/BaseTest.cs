#define TEST_DEBUG_MODE // Run Playwright in non-headless mode for debugging purposes (ie show a browser)

// In docker (Ubuntu container) Playwright will fail if running in non-headless mode, so we ensure TEST_DEBUG_MODE is undef'ed
#if !DEBUG
#undef TEST_DEBUG_MODE
#endif

using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.DependencyInjection;

namespace CdrAuthServer.E2ETests
{
    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("E2ETests")]
    [TestCaseOrderer("CdrAuthServer.E2ETests.XUnit.Orderers.AlphabeticalOrderer", "CdrAuthServer.E2ETests")]
    public abstract class BaseTest : SharedBaseTest
    {
        protected BaseTest(ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
        }
    }
}
