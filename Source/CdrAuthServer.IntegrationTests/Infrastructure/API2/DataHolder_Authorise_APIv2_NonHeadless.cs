using System;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

#nullable enable

namespace CdrAuthServer.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_Authorise_APIv2_NonHeadless : DataHolder_Authorise_APIv2_Base
    {
        /// <summary>
        /// Perform authorisation and consent flow. Returns authCode and idToken
        /// </summary>
        override public async Task<(string authCode, string idToken)> Authorise(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS) 
        {
            // Create cookie container since we need to share cookies across multiple requests
            var cookieContainer = new CookieContainer();

            // Call authorise endpoint, it will redirect to DataHolder login endpoint
            var authResponse = await IdentityServer_Authorise(cookieContainer);

            // Set userid and postback
            var userIdResponse = await DataHolder_Login_UserId(cookieContainer, authResponse);

            // Set password and postback, it will validate user then redirect to IdentityServer which will redirect to DataHolder consent endpoint
            var passwordResponse = await DataHolder_Login_Password(cookieContainer, userIdResponse);

            // Select accounts to share and postback
            var selectAccountsResponse = await DataHolder_Consent_SelectAccountsToShare(cookieContainer, passwordResponse);

            (var authCode, var idToken) = await DataHolder_Consent_Confirm(cookieContainer, selectAccountsResponse);

            return (authCode, idToken);
        }

        // Create http client with cookie container
        private HttpClient CreateHttpClient(CookieContainer cookieContainer, bool allowAutoRedirect = true)
        {
            var httpClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = true,
                AllowAutoRedirect = allowAutoRedirect,
                UseCookies = true,
                CookieContainer = cookieContainer,
            };

            httpClientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            httpClientHandler.ClientCertificates.Add(new X509Certificate2(CertificateFilename, CertificatePassword, X509KeyStorageFlags.Exportable));
            var httpClient = new HttpClient(httpClientHandler);

            return httpClient;
        }

        // Call the authorisation endpoint, IdentityServer will then redirect to the DataHolder login endpoint
        private async Task<HttpResponseMessage?> IdentityServer_Authorise(CookieContainer cookieContainer)
        {
            var URL = new AuthoriseURLBuilder
            {
                Scope = Scope,
                TokenLifetime = TokenLifetime,
                SharingDuration = SharingDuration,
                RequestUri = RequestUri,
                ClientId = ClientId,
                RedirectURI = RedirectURI,
                JWT_CertificateFilename = JwtCertificateFilename,
                JWT_CertificatePassword = JwtCertificatePassword,
                ResponseType = ResponseType
            }.URL;

            var request = new HttpRequestMessage(HttpMethod.Get, URL);

            BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);

            var response = await CreateHttpClient(cookieContainer).SendAsync(request);

            return response;
        }

        // Handle redirect to DataHolder login endpoint, set userid (customer id) and postback 
        private async Task<HttpResponseMessage?> DataHolder_Login_UserId(CookieContainer cookieContainer, HttpResponseMessage? authResponse)
        {
            if (authResponse == null || authResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(DataHolder_Authorise_APIv2_NonHeadless)}.{nameof(DataHolder_Login_UserId)} - {nameof(authResponse)} not 200OK");
            }

            // Load html
            var html = await authResponse.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Parse the form 
            var formFields = HtmlParser.ParseForm(html, "//form");
            formFields["CustomerId"] = UserId;
            formFields["button"] = "page2";

            // Postback
            var requestUri = authResponse?.RequestMessage?.RequestUri?.AbsoluteUri;
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = HtmlParser.FormUrlEncodedContent(formFields),
            };
            BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);
            var response = await CreateHttpClient(cookieContainer).SendAsync(request);
            return response;
        }

        // Set password and postback, DataHolder will validate user and redirect to IdentityServer which will then redirect to DataHolder consent endpoint
        private async Task<HttpResponseMessage?> DataHolder_Login_Password(CookieContainer cookieContainer, HttpResponseMessage? userIdResponse) //, IEnumerable<string> cookies)
        {
            if (userIdResponse == null || userIdResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(DataHolder_Authorise_APIv2_NonHeadless)}.{nameof(DataHolder_Login_Password)} - {nameof(userIdResponse)} not 200OK");
            }

            // Load html
            var html = await userIdResponse.Content.ReadAsStringAsync();

            // Check that customer id was valid
            if (html.Contains("Incorrect customer ID", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new EDataHolder_Authorise_IncorrectCustomerId();
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Parse the form
            var formFields = HtmlParser.ParseForm(html, "//form");
            formFields["Otp"] = OTP;
            formFields["button"] = "auth";

            // Postback
            var requestUri = userIdResponse?.RequestMessage?.RequestUri?.AbsoluteUri;
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = HtmlParser.FormUrlEncodedContent(formFields)
            };
            BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);
            var response = await CreateHttpClient(cookieContainer).SendAsync(request);
            return response;
        }

        // Select user bank accounts to share and postback 
        private async Task<HttpResponseMessage?> DataHolder_Consent_SelectAccountsToShare(CookieContainer cookieContainer, HttpResponseMessage? passwordResponse) //, IEnumerable<string> cookies)
        {
            if (passwordResponse == null || passwordResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(DataHolder_Authorise_APIv2_NonHeadless)}.{nameof(DataHolder_Consent_SelectAccountsToShare)} - {nameof(passwordResponse)} not 200OK");
            }

            // Load html
            var html = await passwordResponse.Content.ReadAsStringAsync();

            // Check that password was valid
            if (html.Contains("Incorrect one time password", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new EDataHolder_Authorise_IncorrectOneTimePassword();
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Parse the form
            var formFields = HtmlParser.ParseForm(html, "//form");

            // Set selected accounts
            if (SelectedAccountIdsArray != null)
            {
                int i = 0;
                foreach (string selectedAccountId in SelectedAccountIdsArray)
                {
                    formFields[$"SelectedAccountIds[{i++}]"] = selectedAccountId;
                }
            }
            formFields["button"] = "page2";

            // Postback
            string? requestUri = passwordResponse?.RequestMessage?.RequestUri?.AbsoluteUri;
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = HtmlParser.FormUrlEncodedContent(formFields)
            };
            BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);
            var response = await CreateHttpClient(cookieContainer).SendAsync(request);
            return response;
        }

        // Confirm selection of accounts and postback 
        private async Task<(string authCode, string idToken)> DataHolder_Consent_Confirm(CookieContainer cookieContainer, HttpResponseMessage? selectAccountsResponse)
        {
            async Task<(string authCode, string idToken)> Postback(HttpRequestMessage request)
            {
                // Upon postback of consent, IdentityServer will redirect to the Data Recipient's redirect url with the authcode etc
                // We need to start a webhost (DataRecipientConsentCallback) to catch the callback
                var callback = new DataRecipientConsentCallback(redirectUrl: RedirectURI);
                callback.Start();
                try
                {
                    // The redirect will happen once we post the callback
                    BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);
                    var response = await CreateHttpClient(cookieContainer).SendAsync(request);

                    var fragment = response.RequestMessage?.RequestUri?.Fragment;
                    if (fragment == null)
                    {
                        throw new Exception($"{nameof(DataHolder_Consent_Confirm)}.{nameof(Postback)} - failed");
                    }

                    var query = HttpUtility.ParseQueryString(fragment.TrimStart('#'));

                    Exception RaiseException(string errorMessage, string? authCode, string? idToken)
                    {
                        var responseRequestUri = response?.RequestMessage?.RequestUri;
                        return new SecurityException($"{errorMessage}\r\nauthCode={authCode},idToken={idToken},response.RequestMessage.RequestUri={responseRequestUri}");
                    }

                    var authCode = query["code"];
                    var idToken = query["id_token"];

                    if (authCode == null)
                    {
                        throw RaiseException("authCode is null", authCode, idToken);
                    }

                    if (idToken == null)
                    {
                        throw RaiseException("idToken is null", authCode, idToken);
                    }

                    var state = query["state"];
                    var nonce = query["nonce"];
                    var scope = query["scope"];

                    return (authCode, idToken);
                }
                finally
                {
                    await callback.Stop();
                }
            }

            if (selectAccountsResponse == null || selectAccountsResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(DataHolder_Authorise_APIv2_NonHeadless)}.{nameof(DataHolder_Consent_Confirm)} - {nameof(selectAccountsResponse)} not 200OK");
            }

            // Load html
            var html = await selectAccountsResponse.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Parse the form
            var formFields = HtmlParser.ParseForm(html, "//form");
            formFields.Remove("SelectedAccountIds");
            for (int i = 0; i < 99; i++)
            {
                formFields.Remove($"SelectedAccountIds[{i}]");
            }
            if (SelectedAccountIdsArray != null)
            {
                int i = 0;
                foreach (string selectedAccountId in SelectedAccountIdsArray)
                {
                    formFields[$"SelectedAccountIds[{i++}]"] = selectedAccountId;
                }
            }
            formFields["button"] = "consent";

            // Postback
            string? requestUri = selectAccountsResponse?.RequestMessage?.RequestUri?.AbsoluteUri;
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = HtmlParser.FormUrlEncodedContent(formFields)
            };
            (var authCode, var idToken) = await Postback(request);

            return (authCode, idToken);
        }
    }
}
