// FIXME - MJS - This is a workaround until the authorization UI is written, then reinstate DataHolder_Authorise_APIv2.cs

using System;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;

#nullable enable

namespace CdrAuthServer.IntegrationTests.Infrastructure.API2
{   
    public class DataHolder_Authorise_APIv2_Headless : DataHolder_Authorise_APIv2_Base
    {
        public async Task<HttpResponseMessage> Authorise2(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS, bool allowRedirect=true)
        {
            var callback = new DataRecipientConsentCallback(redirectUrl);
            callback.Start();
            try
            {
                var cookieContainer = new CookieContainer();
                var response = await IdentityServer_Authorise(cookieContainer, allowRedirect) ?? throw new NullReferenceException();
                return response;
            }
            finally
            {
                await callback.Stop();
            }
        }

        /// <summary>
        /// Perform authorisation and consent flow. Returns authCode and idToken
        /// </summary>
        override public async Task<(string authCode, string idToken)> Authorise(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS)
        {
            var callback = new DataRecipientConsentCallback(redirectUrl);
            callback.Start();
            try
            {
                var cookieContainer = new CookieContainer();

                // "headless" workaround currently "{BaseTest.DH_TLS_IDENTITYSERVER_BASE_URL}/connect/authorize" redirects immediately to the callback uri (ie there's no UI)
                var response = await IdentityServer_Authorise(cookieContainer) ?? throw new NullReferenceException();
               
                // Return authcode and idtoken
                return ExtractAuthCodeIdToken(response);
            }
            finally
            {
                await callback.Stop();
            }

            static (string authCode, string idToken) ExtractAuthCodeIdToken(HttpResponseMessage response)
            {
                var fragment = response.RequestMessage?.RequestUri?.Fragment;
                if (fragment == null)
                {
                    throw new Exception($"{nameof(DataRecipientConsentCallback)} - response fragment is null");
                }

                var query = HttpUtility.ParseQueryString(fragment.TrimStart('#'));

                Exception RaiseException(string errorMessage, string? authCode, string? idToken)
                {
                    var responseRequestUri = response?.RequestMessage?.RequestUri;
                    return new SecurityException($"{errorMessage}\r\nauthCode={authCode},idToken={idToken},response.RequestMessage.RequestUri={responseRequestUri}");
                }

                string? authCode = query["code"];
                string? idToken = query["id_token"];

                if (authCode == null)
                {
                    throw RaiseException("authCode is null", authCode, idToken);
                }

                if (idToken == null)
                {
                    throw RaiseException("idToken is null", authCode, idToken);
                }

                return (authCode, idToken);
            }
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

        private async Task<HttpResponseMessage?> IdentityServer_Authorise(CookieContainer cookieContainer, bool allowRedirect = true)
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

            var response = await CreateHttpClient(cookieContainer, allowRedirect).SendAsync(request);

            return response;
        }       
    }
}
