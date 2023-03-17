using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CdrAuthServer.E2ETests
{
    public class AATestPlaywrightInstallation : BaseTest_v2, IClassFixture<TestFixture>
    {
        // FIXME - MJS - temporarily disabled - this test fails sometimes because google closes connection, need to implement retries
        // [Fact]
        // public async Task ShouldDisplayGoogleHomePage()
        // {
        //     await TestAsync($"{nameof(AATestPlaywrightInstallation)} - {nameof(ShouldDisplayGoogleHomePage)}", async (page) =>
        //     {
        //         // Act - Goto Google.com
        //         await page.GotoAsync("https://www.google.com");

        //         // Assert
        //         await page.ClickAsync(":nth-match(:text(\"I'm Feeling Lucky\"), 2)");
        //     });
        // }
    }
}