using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.UI;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.UI.Pages.Authorisation;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;

namespace CdrAuthServer.E2ETests
{
    public class US39327_CdrAuthServer_E2ETests : BaseTest, IClassFixture<PlaywrightFixture>, IClassFixture<RegisterSoftwareProductFixture>, IAsyncLifetime
    {
        public US39327_CdrAuthServer_E2ETests(
            IOptions<TestAutomationOptions> options,
            ISqlQueryService sqlQueryService,
            IDataHolderParService dataHolderParService,
            IDataHolderRegisterService dataHolderRegisterService,
            IDataHolderAccessTokenCache dataHolderAccessTokenCache,
            IApiServiceDirector apiServiceDirector,
            ITestOutputHelperAccessor testOutputHelperAccessor,
            Microsoft.Extensions.Configuration.IConfiguration config)
            : base(testOutputHelperAccessor, config
            )
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _dataHolderRegisterService = dataHolderRegisterService ?? throw new ArgumentNullException(nameof(dataHolderRegisterService));
            _dataHolderAccessTokenCache = dataHolderAccessTokenCache ?? throw new ArgumentNullException(nameof(dataHolderAccessTokenCache));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        PlaywrightDriver _playwrightDriver = new PlaywrightDriver();
        IBrowserContext? _browserContext;

        public const string CUSTOMERID_BANKING = "ksmith";
        public const string DEFAULT_OPT = "000789";

        private readonly TestAutomationOptions _options;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;
        private readonly IDataHolderAccessTokenCache _dataHolderAccessTokenCache;
        private readonly IApiServiceDirector _apiServiceDirector;

        [Theory]
        [InlineData(ResponseMode.FormPost)]
        public async Task AC01_Authorize_HybridFlow_ShouldRespondWith_AuthCodeAndIdToken(ResponseMode responseMode)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(responseMode), responseMode);

            // Arrange
            Uri authRedirect = await Authorise(
                ResponseType.CodeIdToken, // ie Hybrid flow
                responseMode,
                _options.SCOPE);
            var authRedirectLeftPart = authRedirect.GetLeftPart(UriPartial.Authority) + "/ui";

            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(AC01_Authorize_HybridFlow_ShouldRespondWith_AuthCodeAndIdToken)} - {responseMode}");

            var page = await _browserContext.NewPageAsync();

            await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

            // Username
            AuthenticateLoginPage authenticateLoginPage = new(page);
            await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
            await authenticateLoginPage.ClickContinue();

            // OTP
            OneTimePasswordPage oneTimePasswordPage = new(page);
            await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
            await oneTimePasswordPage.ClickContinue();

            // Select accounts
            SelectAccountsPage selectAccountsPage = new(page);
            await selectAccountsPage.SelectAccounts("Personal Loan");
            await selectAccountsPage.ClickContinue();

            // Confirmation - Click authorise and check callback response
            ConfirmAccountSharingPage confirmAccountSharingPage = new(page);
            (string? code, string? idtoken) = await HybridFlow_HandleCallback(responseMode: responseMode, page: page, setup: async (page) =>
            {
                await confirmAccountSharingPage.ClickAuthorise();
            });

            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                code.Should().NotBeNullOrEmpty();
                idtoken.Should().NotBeNullOrEmpty();
            }

        }

        [Fact]
        public async Task ACX02_Cancel_Otp_And_Verify_Callback()
        {
            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, _options.SCOPE);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX02_Cancel_Otp_And_Verify_Callback)}");
            var page = await _browserContext.NewPageAsync();

            // Act        
            await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

            AuthenticateLoginPage authenticateLoginPage = new(page);
            await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
            await authenticateLoginPage.ClickContinue();

            OneTimePasswordPage oneTimePasswordPage = new(page);
            await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);

            // Assert
            await AssertCancelCallback(async () => await oneTimePasswordPage.ClickCancel());

        }

        [Fact]
        public async Task ACX03_Cancel_Select_Accounts_And_Verify_Callback()
        {
            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, _options.SCOPE);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX03_Cancel_Select_Accounts_And_Verify_Callback)}");
            var page = await _browserContext.NewPageAsync();

            // Act        
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

        }

        [Fact]
        public async Task ACX04_Deny_Account_Confirmation_And_Verify_Callback()
        {
            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, _options.SCOPE);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX04_Deny_Account_Confirmation_And_Verify_Callback)}");
            var page = await _browserContext.NewPageAsync();

            // Act        
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

        }

        [Theory]
        [InlineData("Invalid Customer ID", "foo", "Invalid Customer ID")]
        [InlineData("Missing Customer ID", "", "Customer ID is required")]
        public async Task ACX05_Invalid_Customer_Id(string testSuffix, string customerIdToEnter, string expectedError)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(testSuffix), testSuffix, nameof(customerIdToEnter), customerIdToEnter, nameof(expectedError), expectedError);

            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, _options.SCOPE);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX05_Invalid_Customer_Id)} - {testSuffix}");
            var page = await _browserContext.NewPageAsync();

            // Act        
            await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

            AuthenticateLoginPage authenticateLoginPage = new(page);
            await authenticateLoginPage.EnterCustomerId(customerIdToEnter);
            await authenticateLoginPage.ClickContinue();

            // Assert
            bool invalidCustomerIdMessageExists = await authenticateLoginPage.CustomerIdErrorExists(expectedError);
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                invalidCustomerIdMessageExists.Should().Be(true, $"Customer Id of '{customerIdToEnter}' was entered and expected '{expectedError}'.");
            }

        }

        [Theory]
        [InlineData("Invalid One Time Password", "foo", "Invalid One Time Password")]
        [InlineData("Missing One Time Password", "", "One Time Password is required")]
        public async Task ACX06_Invalid_One_Time_Password(string testSuffix, string otpToEnter, string expectedError)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(testSuffix), testSuffix, nameof(otpToEnter), otpToEnter, nameof(expectedError), expectedError);
            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, _options.SCOPE);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX06_Invalid_One_Time_Password)} - {testSuffix}");
            var page = await _browserContext.NewPageAsync();

            // Act        
            await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

            AuthenticateLoginPage authenticateLoginPage = new(page);
            await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
            await authenticateLoginPage.ClickContinue();

            OneTimePasswordPage oneTimePasswordPage = new(page);
            await oneTimePasswordPage.EnterOtp(otpToEnter);
            await oneTimePasswordPage.ClickContinue();

            // Assert
            bool invalidOptMessageExists = await oneTimePasswordPage.OtpErrorExists(expectedError);
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                invalidOptMessageExists.Should().Be(true, $"OTP of '{otpToEnter}' was entered and expected '{expectedError}'.");
            }

        }

        [Fact]
        public async Task ACX07_No_Accounts_Selected()
        {
            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, _options.SCOPE);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX06_Invalid_One_Time_Password)}");
            var page = await _browserContext.NewPageAsync();

            // Act        
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
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                noAccountSelectedErrorExists.Should().Be(true, $"No accounts were selected in the account selection form.");
            }

        }

        // The test below aims to cover all Common, Banking and Energy scopes. Including the merging of Basic and Detailed clusters.
        [Theory]
        //Common
        [InlineData("AC01-3AU.02.14 Common Basic with Profile", "openid profile common:customer.basic:read cdr:registration",
            new ClusterType[] { ClusterType.CommonName, ClusterType.CommonNameAndOccupation })]
        [InlineData("AC02-3AU.02.14 Common Basic with Detailed Common", "openid common:customer.basic:read common:customer.detail:read cdr:registration",
            new ClusterType[] { ClusterType.CommonNameAndOccupation, ClusterType.CommonContactDetails })]
        [InlineData("AC03-3AU.02.14 Common Detailed Common Only", "openid common:customer.detail:read cdr:registration",
            new ClusterType[] { ClusterType.CommonNameOccupationAndContactDetails })]
        //Banking
        [InlineData("AC04-3AU.02.14 Bank Detailed with Bank Basic", "openid bank:accounts.detail:read bank:accounts.basic:read",
            new ClusterType[] { ClusterType.BankAccountNameTypeAndBalance, ClusterType.BankAccountNumbersAndFeatures })]
        [InlineData("AC05-3AU.02.14 Bank Detailed Only", "openid bank:accounts.detail:read",
            new ClusterType[] { ClusterType.BankAccountBalanceAndDetails })]
        [InlineData("AC06-3AU.02.14 Bank Detailed with Bank Transations", "openid bank:accounts.detail:read bank:transactions:read cdr:registration",
            new ClusterType[] { ClusterType.BankAccountBalanceAndDetails,
                ClusterType.BankTransactionDetails})]
        [InlineData("AC07-3AU.02.14", "openid bank:regular_payments:read",
            new ClusterType[] { ClusterType.BankDirectDebitAndSheduledPayments })]
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
            new ClusterType[] { ClusterType.EnergyAccountsAndPlans, ClusterType.EnergyAccountAndPlanDetailsWithBasicScope })]
        [InlineData("AC11-3AU.02.14 Energy Detailed Only", "openid energy:accounts.detail:read",
            new ClusterType[] { ClusterType.EnergyAccountAndPlanDetailsWithoutBasicScope })]
        [InlineData("AC12-3AU.02.14 Energy connection and meter", "openid energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read",
            new ClusterType[] { ClusterType.EnergyElectricityConnection, ClusterType.EnergyElectricityMeter })]
        [InlineData("AC13-3AU.02.14 Energy Consessions-Payments-Billing",
            "openid energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read",
            new ClusterType[] { ClusterType.EnergyConcessionsAndAssistance, ClusterType.EnergyPaymentPreferences, ClusterType.EnergyEnergyBillingPaymentsAndHistory })]
        [InlineData("AC14-3AU.02.14 Energy DER-Usage", "openid energy:electricity.der:read energy:electricity.usage:read",
            new ClusterType[] { ClusterType.EnergyEnergyGenerationAndStorage, ClusterType.EnergyElectricityUsage })]

        public async Task ACX08_Confirmation_UI_Cluster_Verification(string testSuffix, string actualScope, ClusterType[] expectedClusters)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}, {P3}={V3}.", nameof(testSuffix), testSuffix, nameof(actualScope), actualScope, nameof(expectedClusters), expectedClusters);

            // Arrange
            Uri authRedirect = await Authorise(ResponseType.CodeIdToken, ResponseMode.FormPost, scope: actualScope);
            _browserContext = await _playwrightDriver.NewBrowserContext($"{nameof(ACX08_Confirmation_UI_Cluster_Verification)} - {testSuffix}");
            var page = await _browserContext.NewPageAsync();

            // Act        

            await page.GotoAsync(authRedirect.AbsoluteUri); // redirect user to Auth UI to login and consent to share accounts

            AuthenticateLoginPage authenticateLoginPage = new(page);
            await authenticateLoginPage.EnterCustomerId(CUSTOMERID_BANKING);
            await authenticateLoginPage.ClickContinue();

            OneTimePasswordPage oneTimePasswordPage = new(page);
            await oneTimePasswordPage.EnterOtp(DEFAULT_OPT);
            await oneTimePasswordPage.CloseAlertMessage();
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

            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                actualClusterCount.Should().Be(expectedClusters.Length, because: "Actual count of cluster sshould match expected.");
            }
        }

        internal static async Task VerifyCluster(ClusterType clusterType, ConfirmAccountSharingPage confirmAccountSharingPage)
        {
            (string expectedHeading, string expectedDetail) = GetExpectedClusterDetail(clusterType);

            await confirmAccountSharingPage.ClickCLusterHeadingToExpand(expectedHeading);
            string actualClusterDetail = await confirmAccountSharingPage.GetClusterDetail(expectedHeading);

            // Remove newline chars from actual and expected to ensure consistency in results.
            // When running in a docker container, the last element does not always have a newline delimiter.
            Assert.Equal(expectedDetail.Replace("\n", ""), actualClusterDetail.Replace("\n", ""));
        }

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
        private async Task<Uri> Authorise(ResponseType responseType, ResponseMode responseMode, string scope)
        {
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options, true);

            var clientId = _options.LastRegisteredClientId;

            var requestUri = await _dataHolderParService.GetRequestUri(clientId: clientId, responseMode: responseMode, scope: scope);

            var queryString = new Dictionary<string, string?>
            {
                { "request_uri", requestUri },
                { "response_type", responseType.ToEnumMemberAttrValue() },
                { "response_mode", responseMode.ToEnumMemberAttrValue() },
                { "client_id", clientId },
                { "redirect_uri", _options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS },
                { "scope", scope },
            };

            var api = _apiServiceDirector.BuildAuthServerAuthorizeAPI(queryString);
            var response = await api.SendAsync();

            var redirectlocation = response.Headers.Location;

            if (response.StatusCode != HttpStatusCode.Redirect)
            {
                throw new Exception($"Expected {HttpStatusCode.Redirect} but {response.StatusCode}");
            }

            return response.Headers.Location ?? throw new NullReferenceException(nameof(response.Headers.Location.AbsoluteUri));
        }

        delegate Task HybridFlow_HandleCallback_Setup(IPage page);
        private async Task<(string? code, string? idtoken)> HybridFlow_HandleCallback(ResponseMode responseMode, IPage page, HybridFlow_HandleCallback_Setup setup)
        {
            var callback = new DataRecipientConsentCallback(_options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
            callback.Start();
            try
            {
                await setup(page);

                var callbackRequest = await callback.WaitForCallback();
                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    switch (responseMode)
                    {
                        case ResponseMode.FormPost:
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
                        case ResponseMode.Fragment:
                            {
                                callbackRequest.Should().NotBeNull();
                                callbackRequest?.received.Should().BeTrue();
                                callbackRequest?.method.Should().Be(HttpMethod.Get);
                                throw new NotImplementedException("FIXME - MJS - check request URL fragment for authcode & idtoken");
                            }
                        case ResponseMode.Query:
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
            }
            finally
            {
                await callback.Stop();
            }
        }

        delegate Task CancelAction();
        private async Task AssertCancelCallback(CancelAction cancelAction)
        {

            var callback = new DataRecipientConsentCallback(_options.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
            callback.Start();
            try
            {
                await cancelAction();

                var callbackRequest = await callback.WaitForCallback();

                using (new AssertionScope(BaseTestAssertionStrategy))
                {
                    callbackRequest.Should().NotBeNull();
                    callbackRequest?.received.Should().BeTrue();
                    callbackRequest?.method.Should().Be(HttpMethod.Post);
                    callbackRequest?.body.Should().NotBeNullOrEmpty();

                    var body = QueryHelpers.ParseQuery(callbackRequest?.body);
                    string? error = body["error"];
                    string? errorDescription = body["error_description"];
                    error.Should().Be("access_denied");
                    errorDescription.Should().Be("ERR-AUTH-009: User cancelled the authorisation flow");
                }
            }
            finally
            {
                await callback.Stop();
            }
        }
        public static (string expectedHeading, string expectedDetail) GetExpectedClusterDetail(ClusterType scopesType)
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

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (_browserContext != null)
            {
                await _playwrightDriver.DisposeAsync();
            }
        }
    }
}
