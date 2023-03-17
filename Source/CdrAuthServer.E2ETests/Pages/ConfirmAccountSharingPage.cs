using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.E2ETests.Pages
{
    internal class ConfirmAccountSharingPage
    {
        private readonly IPage _page;
        private readonly ILocator _btnAuthorise;
        private readonly ILocator _btnDeny;


        public ConfirmAccountSharingPage(IPage page)
        {
            _page = page;
            _btnAuthorise = _page.Locator("text=Authorise");
            _btnDeny = _page.Locator("text=Deny");
        }

        public async Task ClickAuthorise()
        {
            await _btnAuthorise.ClickAsync();
        }
        public async Task ClickDeny()
        {
            await _btnDeny.ClickAsync();
        }

        public async Task ClickCLusterHeadingToExpand(string clusterHeading)
        {
            await _page.Locator($"//a[text()='{clusterHeading}']").ClickAsync();
        }

        public async Task<string> GetClusterDetail(string clusterHeading)
        {
            return await _page.Locator($"//div[@role='button' and .//a[text()='{clusterHeading}']]/..//p").InnerTextAsync();
        }

        public async Task<int> GetClusterCount()
        {
            var allClusterHeadings = await _page.QuerySelectorAllAsync("//div[contains(@class,'MuiAccordionSummary')]/a");
            return allClusterHeadings.Count;
        }

    }
}
