using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CdrAuthServer.E2ETests.Pages
{
    internal class AuthenticateLoginPage
    {
        private readonly IPage _page;
        private readonly ILocator _txtCustomerId;
        private readonly ILocator _btnCancel;
        private readonly ILocator _btnContinue;
        private readonly ILocator _lblCustomerIdErrorMessage;        
        private readonly ILocator _lblHelpForExampleUserNames;


        public AuthenticateLoginPage(IPage page) 
        {
            _page = page;
            _txtCustomerId = _page.Locator("id=mui-1");
            _btnCancel = _page.Locator("text=Cancel");
            _btnContinue = _page.Locator("button:has-text(\"Continue\")");
            _lblHelpForExampleUserNames = _page.Locator("//div[@role='alert']");
        }

        public async Task EnterCustomerId(string customerId)
        {
            await _txtCustomerId.WaitForAsync();
            await Task.Delay(1000); //require for JS delayed defaulting of field. It can sometimes overwrite the entered value.
            await _txtCustomerId.FillAsync("");
            await _txtCustomerId.FillAsync(customerId);
        }

        public async Task ClickContinue()
        {
            await _btnContinue.ClickAsync();
        }

        public async Task<string> GetError()
        {
            return await _lblCustomerIdErrorMessage.TextContentAsync();
        }

        public async Task<string> GetHelpForExampleUserNamesText()
        {
            return await _lblHelpForExampleUserNames.TextContentAsync();
        }

        public async Task<bool> CustomerIdErrorExists(string errorToCheckFor)
        {       
            try
            {
                var element = await _page.WaitForSelectorAsync($"//p[text()='{errorToCheckFor}']");
                return await element.IsVisibleAsync();
            }
            catch (TimeoutException) { }
            {
                return false;
            }
        }

    }
}
