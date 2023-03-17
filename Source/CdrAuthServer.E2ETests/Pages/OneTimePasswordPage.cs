using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.E2ETests.Pages
{
    internal class OneTimePasswordPage
    {
        private readonly IPage _page;
        private readonly ILocator _txtOneTimePassword;
        private readonly ILocator _btnCancel;
        private readonly ILocator _btnContinue;
        private readonly ILocator _divAlert;
        private readonly ILocator _headDataHolderheading;
        private readonly ILocator _btnCloseAlert;

        public OneTimePasswordPage(IPage page)
        {
            _page = page;
            _txtOneTimePassword = _page.Locator("id=mui-2");
            _btnCancel = _page.Locator("text=Cancel");
            _btnContinue = _page.Locator("button:has-text(\"Continue\")");
            _divAlert = _page.Locator("//div[@role='alert']");
            _headDataHolderheading = _page.Locator("//h6"); 
            _btnCloseAlert = _page.Locator("[role=\"alert\"]>>[title=\"Close\"]");

        }
        public async Task EnterOtp(string otp)
        {
            await _txtOneTimePassword.WaitForAsync();
            await _txtOneTimePassword.FillAsync(otp);
        }

        public async Task ClickContinue()
        {
            await _btnContinue.ClickAsync();
        }

        public async Task ClickCancel()
        {
            await _btnCancel.ClickAsync();
        }

        public async Task<string> GetOneTimePasswordFieldValue()
        {
            return await _txtOneTimePassword.InputValueAsync();
        }

        public async Task<string> GetAlertMessage()
        {
            return await _divAlert.InnerTextAsync();
        }

        public async Task<string> GetDataHolderHeading()
        {
            return await _headDataHolderheading.InnerTextAsync();
        }

        public async Task<bool> AlertExists()
        {
            return await _divAlert.IsVisibleAsync();
        }

        public async Task CloseAlertMessage()
        {
            await _btnCloseAlert.ClickAsync();
        }

        public async Task<bool> OtpErrorExists(string errorToCheckFor)
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
