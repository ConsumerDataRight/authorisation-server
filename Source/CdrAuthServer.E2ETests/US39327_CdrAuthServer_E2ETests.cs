using CdrAuthServer.E2ETests.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CdrAuthServer.E2ETests
{
    public class US39327_CdrAuthServer_E2ETests : BaseTest_v2, IClassFixture<TestFixture>, IClassFixture<IntegrationTests.Fixtures.RegisterSoftwareProductFixture>
    {
        private string redirectURI => IntegrationTests.BaseTest.SubstituteConstant(IntegrationTests.BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
        public const string CUSTOMERID_BANKING = "ksmith";
        public const string DEFAULT_OPT = "000789";
        
        public enum ClusterType
        {
            CommonName,
            BankAccountNameTypeAndBalance,
            BankAccountNumbersAndFeatures,
            BankAccountBalanceAndDetails,           
            BankTransactionDetails,
            BankDirectDebitAndSheduledPayments,
            BankSavedPayees,
            EnergyAccountsAndPlans,
            EnergyAccountAndPlanDetailsWithBasicScope,
            EnergyAccountAndPlanDetailsWithoutBasicScope,
            EnergyConcessionsAndAssistance,
            EnergyPaymentPreferences,
            EnergyEnergyBillingPaymentsAndHistory,
            EnergyElectricityConnection,
            EnergyElectricityMeter,
            EnergyElectricityConnectionAndMeter,
            EnergyEnergyGenerationAndStorage,
            EnergyElectricityUsage,
            CommonNameAndOccupation,
            CommonContactDetails,
            CommonNameOccupationAndContactDetails
        }

        // Call authorise endpoint, should respond with a redirect to UI, return the redirect URI
        private async Task<Uri> Authorise(string responseType, string responseMode, string scope = IntegrationTests.BaseTest.SCOPE)
        {
            IntegrationTests.Fixtures.TestSetup.DataHolder_PurgeIdentityServer(true);

            var clientId = IntegrationTests.BaseTest.GetClientId(IntegrationTests.BaseTest.SOFTWAREPRODUCT_ID);

            var requestUri = await IntegrationTests.BaseTest.PAR_GetRequestUri(clientId: clientId, responseMode: responseMode, scope: scope);

            var queryString = new Dictionary<string, string?>
            {
                { "request_uri", requestUri },
                { "response_type", responseType },
                { "response_mode", responseMode },
                { "client_id", clientId },
                { "redirect_uri", redirectURI },
                { "scope", scope },
            };

            var api = new IntegrationTests.Infrastructure.API
            {
                CertificateFilename = IntegrationTests.BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = IntegrationTests.BaseTest.CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = QueryHelpers.AddQueryString($"{IntegrationTests.BaseTest.DH_TLS_IDENTITYSERVER_BASE_URL}/connect/authorize", queryString),
            };
            var response = await api.SendAsync();

            var redirectlocation = response.Headers.Location;

            if (response.StatusCode != HttpStatusCode.Redirect)
            {
                throw new Exception($"Expected {HttpStatusCode.Redirect} but {response.StatusCode}");
            }

            return response.Headers.Location ?? throw new NullReferenceException(nameof(response.Headers.Location.AbsoluteUri));
        }

        delegate Task HybridFlow_HandleCallback_Setup(IPage page);
        private async Task<(string code, string idtoken)> HybridFlow_HandleCallback(string responseMode, IPage page, HybridFlow_HandleCallback_Setup setup)
        {
            var callback = new IntegrationTests.Infrastructure.API2.DataRecipientConsentCallback(redirectURI);
            callback.Start();
            try
            {
                await setup(page);

                var callbackRequest = await callback.WaitForCallback();
                switch (responseMode)
                {
                    case "form_post":
                        {
                            callbackRequest.Should().NotBeNull();
                            callbackRequest?.received.Should().BeTrue();
                            callbackRequest?.method.Should().Be(HttpMethod.Post);
                            callbackRequest?.body.Should().NotBeNullOrEmpty();

                            var body = QueryHelpers.ParseQuery(callbackRequest?.body);
                            var code = body["code"];
                            var id_token = body["id_token"];

                            code.Should().NotBeNullOrEmpty();
                            id_token.Should().NotBeNullOrEmpty();

                            return (code, id_token);
                        }
                    case "fragment":
                        {
                            callbackRequest.Should().NotBeNull();
                            callbackRequest?.received.Should().BeTrue();
                            callbackRequest?.method.Should().Be(HttpMethod.Get);
                            throw new NotImplementedException("FIXME - MJS - check request URL fragment for authcode & idtoken");                            
                        }
                    case "query":
                        {
                            callbackRequest.Should().NotBeNull();
                            callbackRequest?.received.Should().BeTrue();
                            callbackRequest?.method.Should().Be(HttpMethod.Get);
                            throw new NotImplementedException("FIXME - MJS - check request URL query string for authcode & idtoken");                            
                        }
                    default:
                        throw new NotSupportedException(nameof(responseMode));
                }
            }
            finally
            {
                await callback.Stop();
            }
        }

        delegate Task CancelAction();
        private async Task AssertCancelCallback(CancelAction cancelAction )
        {

            var callback = new IntegrationTests.Infrastructure.API2.DataRecipientConsentCallback(redirectURI);
            callback.Start();
            try
            {
                await cancelAction();

                var callbackRequest = await callback.WaitForCallback();

                callbackRequest.Should().NotBeNull();
                callbackRequest?.received.Should().BeTrue();
                callbackRequest?.method.Should().Be(HttpMethod.Post);
                callbackRequest?.body.Should().NotBeNullOrEmpty();

                var body = QueryHelpers.ParseQuery(callbackRequest?.body);
                string error = body["error"];
                string errorDescription = body["error_description"];
                error.Should().Be("access_denied");
                errorDescription.Should().Be("ERR-AUTH-009: User cancelled the authorisation flow");

            }
            finally
            {
                await callback.Stop();
            }
        }

        [Theory]
        [InlineData("form_post")]
        public async Task AC01_Authorize_HybridFlow_ShouldRespondWith_AuthCodeAndIdToken(string responseMode)
        {
            // Arrange
            Uri authRedirect = await Authorise(
                "code id_token", // ie Hybrid flow
                responseMode);
            var authRedirectLeftPart = authRedirect.GetLeftPart(UriPartial.Authority) + "/ui";

            // Act        
            await TestAsync($"{nameof(US39327_CdrAuthServer_E2ETests)} - {nameof(AC01_Authorize_HybridFlow_ShouldRespondWith_AuthCodeAndIdToken)} - {responseMode}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                await page.Locator("h6:has-text(\"Mock Data Holder Banking\")").TextContentAsync();
                await page.Locator("h5:has-text(\"Login\")").TextContentAsync();

                // Username
                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                // OTP
                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
                await oneTimePasswordPage.ClickContinue();

                // Select accounts
                await page.WaitForURLAsync($"{authRedirectLeftPart}/select-accounts");
                SelectAccountsPage selectAccountsPage = new(page);
                await selectAccountsPage.SelectAccounts("Personal Loan");
                await selectAccountsPage.ClickContinue();

                // Confirmation - Click authorise and check callback response
                await page.WaitForURLAsync($"{authRedirectLeftPart}/confirmation");
                ConfirmAccountSharingPage confirmAccountSharingPage = new(page);                

                (string code, string idtoken) = await HybridFlow_HandleCallback(responseMode: responseMode, page: page, setup: async (page) =>
                {
                    await confirmAccountSharingPage.ClickAuthorise();
                });
                code.Should().NotBeNullOrEmpty();
                idtoken.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public async Task ACX02_Cancel_Otp_And_Verify_Callback()
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post");

            // Act        
            await TestAsync($"{nameof(ACX02_Cancel_Otp_And_Verify_Callback)}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);

                // Assert
                await AssertCancelCallback(async () => await oneTimePasswordPage.ClickCancel());

            });

        }

        [Fact]
        public async Task ACX03_Cancel_Select_Accounts_And_Verify_Callback()
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post");

            // Act        
            await TestAsync($"{nameof(ACX03_Cancel_Select_Accounts_And_Verify_Callback)}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
                await oneTimePasswordPage.ClickContinue();

                SelectAccountsPage selectAccountsPage = new(page);
                await selectAccountsPage.SelectAccounts("Personal Loan 3, Personal Loan 6");

                // Assert
                await AssertCancelCallback(async () => await selectAccountsPage.ClickCancel());
                
            });

        }

        [Fact]
        public async Task ACX04_Deny_Account_Confirmation_And_Verify_Callback()
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post");

            // Act        
            await TestAsync($"{nameof(ACX04_Deny_Account_Confirmation_And_Verify_Callback)}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
                await oneTimePasswordPage.ClickContinue();

                SelectAccountsPage selectAccountsPage = new(page);
                await selectAccountsPage.SelectAccounts("Personal Loan 3, Personal Loan 6");
                await selectAccountsPage.ClickContinue();

                ConfirmAccountSharingPage confirmAccountSharingPage = new(page);

                // Assert
                await AssertCancelCallback(async () => await confirmAccountSharingPage.ClickDeny());

            });

        }

        [Theory]
        [InlineData("Invalid Customer ID", "foo", "Invalid Customer ID")]
        [InlineData("Missing Customer ID", "", "Customer ID is required")]
        public async Task ACX05_Invalid_Customer_Id(string testSuffix, string customerIdToEnter, string expectedError)
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post");

            // Act        
            await TestAsync($"{nameof(ACX05_Invalid_Customer_Id)} - {testSuffix}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(customerIdToEnter);
                await authenticateLoginPage.ClickContinue();

                // Assert
                bool invalidCustomerIdMessageExists = await authenticateLoginPage.CustomerIdErrorExists(expectedError);
                invalidCustomerIdMessageExists.Should().Be(true, $"Customer Id of '{customerIdToEnter}' was entered and expected '{expectedError}'.");

            });

        }

        [Theory]
        [InlineData("Invalid One Time Password", "foo", "Invalid One Time Password")]
        [InlineData("Missing One Time Password", "", "One Time Password is required")]
        public async Task ACX06_Invalid_One_Time_Password(string testSuffix, string otpToEnter, string expectedError)
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post");

            // Act        
            await TestAsync($"{nameof(ACX06_Invalid_One_Time_Password)} - {testSuffix}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(otpToEnter);
                await oneTimePasswordPage.ClickContinue();

                // Assert
                bool invalidOptMessageExists = await oneTimePasswordPage.OtpErrorExists(expectedError);
                invalidOptMessageExists.Should().Be(true, $"OTP of '{otpToEnter}' was entered and expected '{expectedError}'.");

            });

        }

        [Fact]
        public async Task ACX07_No_Accounts_Selected()
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post");

            // Act        
            await TestAsync($"{nameof(ACX07_No_Accounts_Selected)}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
                await oneTimePasswordPage.ClickContinue();

                SelectAccountsPage selectAccountsPage = new(page);
                await selectAccountsPage.ClickContinue();

                // Assert
                bool noAccountSelectedErrorExists = await selectAccountsPage.NoAccountSelectedErrorExists();
                noAccountSelectedErrorExists.Should().Be(true, $"No accounts were selected in the account selection form.");

            });

        }

        // The test below aims to cover all Common, Banking and Energy scopes. Including the merging of Basic and Detailed clusters.
        [Theory]
        //Common
        [InlineData("AC01-3AU.02.14 Common Basic with Profile", "openid profile common:customer.basic:read cdr:registration",
            new ClusterType[] { ClusterType.CommonName, ClusterType.CommonNameAndOccupation})]
        [InlineData("AC02-3AU.02.14 Common Basic with Detailed Common", "openid common:customer.basic:read common:customer.detail:read cdr:registration",
            new ClusterType[] { ClusterType.CommonNameAndOccupation, ClusterType.CommonContactDetails })]
        [InlineData("AC03-3AU.02.14 Common Detailed Common Only", "openid common:customer.detail:read cdr:registration",
            new ClusterType[] { ClusterType.CommonNameOccupationAndContactDetails})]
        //Banking
        [InlineData("AC04-3AU.02.14 Bank Detailed with Bank Basic", "openid bank:accounts.detail:read bank:accounts.basic:read",
            new ClusterType[] { ClusterType.BankAccountNameTypeAndBalance, ClusterType.BankAccountNumbersAndFeatures })]
        [InlineData("AC05-3AU.02.14 Bank Detailed Only", "openid bank:accounts.detail:read",
            new ClusterType[] { ClusterType.BankAccountBalanceAndDetails })]
        [InlineData("AC06-3AU.02.14 Bank Detailed with Bank Transations", "openid bank:accounts.detail:read bank:transactions:read cdr:registration",
            new ClusterType[] { ClusterType.BankAccountBalanceAndDetails,
                ClusterType.BankTransactionDetails})]
        [InlineData("AC07-3AU.02.14", "openid bank:regular_payments:read",
            new ClusterType[] { ClusterType.BankDirectDebitAndSheduledPayments})]
        [InlineData("AC08-3AU.02.14", "openid bank:payees:read",
            new ClusterType[] { ClusterType.BankSavedPayees })]
        [InlineData("AC09-3AU.02.14 all bank", "openid bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:regular_payments:read bank:payees:read",
            new ClusterType[] { ClusterType.BankAccountNameTypeAndBalance,
                ClusterType.BankAccountNumbersAndFeatures,
                ClusterType.BankTransactionDetails,
                ClusterType.BankDirectDebitAndSheduledPayments,
                ClusterType.BankSavedPayees})]
        //Energy
        [InlineData("AC10-3AU.02.14 Energy Basic with detailed", "openid energy:accounts.basic:read openid energy:accounts.detail:read",
            new ClusterType[] { ClusterType.EnergyAccountsAndPlans, ClusterType.EnergyAccountAndPlanDetailsWithBasicScope})]
        [InlineData("AC11-3AU.02.14 Energy Detailed Only", "openid energy:accounts.detail:read",
            new ClusterType[] { ClusterType.EnergyAccountAndPlanDetailsWithoutBasicScope})]
        [InlineData("AC12-3AU.02.14 Energy connection and meter", "openid energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read",
            new ClusterType[] { ClusterType.EnergyElectricityConnection, ClusterType.EnergyElectricityMeter })]
        [InlineData("AC13-3AU.02.14 Energy Consessions-Payments-Billing",
            "openid energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read",
            new ClusterType[] { ClusterType.EnergyConcessionsAndAssistance, ClusterType.EnergyPaymentPreferences, ClusterType.EnergyEnergyBillingPaymentsAndHistory })]
        [InlineData("AC14-3AU.02.14 Energy DER-Usage", "openid energy:electricity.der:read energy:electricity.usage:read",
            new ClusterType[] { ClusterType.EnergyEnergyGenerationAndStorage, ClusterType.EnergyElectricityUsage })]

        public async Task ACX08_Confirmation_UI_Cluster_Verification(string testSuffix, string actualScope, ClusterType[] expectedClusters)
        {
            // Arrange
            Uri authRedirect = await Authorise("code id_token", "form_post", scope:actualScope);

            // Act        
            await TestAsync($"{nameof(ACX08_Confirmation_UI_Cluster_Verification)} - {testSuffix}", async (page) =>
            {
                await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

                AuthenticateLoginPage authenticateLoginPage = new(page);
                await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
                await authenticateLoginPage.ClickContinue();

                OneTimePasswordPage oneTimePasswordPage = new(page);
                await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
                await oneTimePasswordPage.ClickContinue();

                SelectAccountsPage selectAccountsPage = new(page);
                await selectAccountsPage.SelectAccounts("Personal Loan 3, Personal Loan 6");
                await selectAccountsPage.ClickContinue();

                ConfirmAccountSharingPage confirmAccountSharingPage = new(page);
                
                //Assert
                foreach (ClusterType c in expectedClusters)
                {
                    await VerifyCluster(c, confirmAccountSharingPage);
                }

                int actualClusterCount = await confirmAccountSharingPage.GetClusterCount();
                actualClusterCount.Should().Be(expectedClusters.Length, because: "Actual count of cluster sshould match expected.");

            });
        }

        internal async Task VerifyCluster(ClusterType clusterType, ConfirmAccountSharingPage confirmAccountSharingPage)
        {
            (string expectedHeading, string expectedDetail) = GetExpectedClusterDetail(clusterType);

            await confirmAccountSharingPage.ClickCLusterHeadingToExpand(expectedHeading);
            string actualClusterDetail = await confirmAccountSharingPage.GetClusterDetail(expectedHeading);
          
            Assert.Equal(expectedDetail, actualClusterDetail);
        }

        public (string expectedHeading, string expectedDetail) GetExpectedClusterDetail(ClusterType scopesType)
        {
            switch (scopesType)
            {
                case ClusterType.CommonName:
                    return ("Name", "Full name and title(s)");

                case ClusterType.CommonNameAndOccupation:
                    return ("Name and occupation", "Name;\nOccupation;");

                case ClusterType.CommonContactDetails:
                    return ("Contact Details", "Phone;\nEmail address;\nMail address;\nResidential address;");

                case ClusterType.CommonNameOccupationAndContactDetails:
                    return ("Name, occupation, contact details", "Name;\nOccupation;\nPhone;\nEmail address;\nMail address;\nResidential address;");

                case ClusterType.BankAccountNameTypeAndBalance:
                    return ("Account name, type and balance", "Name of account;\nType of account;\nAccount balance;");

                case ClusterType.BankAccountNumbersAndFeatures:
                    return ("Account numbers and features", "Account number;\nInterest rates;\nFees;\nDiscounts;\nAccount terms;\nAccount mail address;");

                case ClusterType.BankAccountBalanceAndDetails:
                    return ("Account balance and details", "Name of account;\nType of account;\nAccount balance;\nAccount number;\nInterest rates;\nFees;\nDiscounts;\nAccount terms;\nAccount mail address;");

                case ClusterType.BankTransactionDetails:
                    return ("Transaction details", "Incoming and outgoing transactions;\nAmounts;\nDates;\nDescriptions of transactions;\nWho you have sent money to and received money from;\n(e.g. their name);");

                case ClusterType.BankDirectDebitAndSheduledPayments:
                    return ("Direct debits and scheduled payments", "Direct debits;\nScheduled payments;");

                case ClusterType.BankSavedPayees:
                    return ("Saved payees", "Names and details of accounts you have saved; (e.g. their BSB and Account Number, BPAY CRN and Biller code, or NPP PayID)");

                case ClusterType.EnergyAccountsAndPlans:
                    return ("Accounts and plans", "Account and plan information;");

                case ClusterType.EnergyAccountAndPlanDetailsWithBasicScope:
                    return ("Account and plan details", "Account type;\nFees, features, rates, and discounts;\nAdditional account users;");

                case ClusterType.EnergyAccountAndPlanDetailsWithoutBasicScope:
                    return ("Account and plan details", "Account and plan information;\nAccount type;\nFees, features, rates, and discounts;\nAdditional account users;");

                case ClusterType.EnergyConcessionsAndAssistance:
                    return ("Concessions and assistance", "Concession type;\nConcession information;");

                case ClusterType.EnergyPaymentPreferences:
                    return ("Payment preferences", "Payment and billing frequency;\nAny scheduled payment details;");

                case ClusterType.EnergyEnergyBillingPaymentsAndHistory:
                    return ("Billing payments and history", "Account balance;\nPayment method;\nPayment status;\nCharges, discounts, credits;\nBilling date;\nUsage for billing period;\nPayment date;\nInvoice number;");

                case ClusterType.EnergyElectricityConnection:
                    return ("Electricity connection", "National Meter Identifier (NMI);\nCustomer type;\nConnection point details;");

                case ClusterType.EnergyElectricityMeter:
                    return ("Electricity meter", "Supply address;\nMeter details;\nAssociated service providers;");

                case ClusterType.EnergyElectricityConnectionAndMeter:
                    return ("Electricity connection and meter", "National Meter Identifier (NMI);\nSupply address;\nCustomer type;\nConnection point details;\nMeter details;\nAssociated service providers;");

                case ClusterType.EnergyEnergyGenerationAndStorage:
                    return ("Energy generation and storage", "Generation information;\nGeneration or storage device type;\nDevice characteristics;\nDevices that can operate without the grid;\nEnergy conversion information;");

                case ClusterType.EnergyElectricityUsage:
                    return ("Electricity usage", "Usage;\nMeter details;");

                default:
                    throw new ArgumentException($"{nameof(scopesType)} = {scopesType}");
            }
        }
    }
}
