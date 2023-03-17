using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdrAuthServer.E2ETests.Pages
{
    internal class SelectAccountsPage
    {

        private readonly IPage _page;
        private readonly ILocator _btnContinue;
        private readonly ILocator _btnCancel;

        public SelectAccountsPage(IPage page)
        {
            _page = page;
            _btnContinue = _page.Locator("text=Continue");
            _btnCancel = _page.Locator("text=Cancel");
        }

        public async Task SelectAccount(string accountToSelect)
        {
            await _page.Locator($"//input[@aria-labelledby='account-{accountToSelect}']").CheckAsync();
        }

        public async Task SelectAccounts(string accountsToSelectCsv)
        {
            string[] accountsToSelectArray = accountsToSelectCsv?.Split(",");

            foreach (string accountToSelect in accountsToSelectArray)
            {
                await SelectAccount(accountToSelect.Trim());
            }

        }

        public async Task SelectAllCheckboxes()
        {
            var allInputs = await _page.QuerySelectorAllAsync("//input[@type='checkbox']");

            foreach (var input in allInputs)
            {
                await input.CheckAsync();
            }

        }

        public async Task ClickContinue()
        {
            await _btnContinue.ClickAsync();
        }
        public async Task ClickCancel()
        {
            await _btnCancel.ClickAsync();
        }
        public async Task<bool> NoAccountSelectedErrorExists()
        {
            try
            {
                var element = await _page.WaitForSelectorAsync($"//p[text()='Please select one or more Accounts']");
                return await element.IsVisibleAsync();
            }
            catch (TimeoutException) { }
            {
                return false;
            }
        }

    }
}
