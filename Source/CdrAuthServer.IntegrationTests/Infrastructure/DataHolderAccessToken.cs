using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System;

namespace CdrAuthServer.IntegrationTests
{
    public class DataHolderTokenResponse
    {
        static public async Task<DataHolderTokenResponse> Deserialize(HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<DataHolderTokenResponse>(content);
        }

#nullable disable // FIXME - MJS - fix nullable warnings
        [JsonProperty("id_token")]
        public string IdToken { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("cdr_arrangement_id")]
        public string CdrArrangementId { get; set; }
#nullable enable // FIXME - MJS - fix nullable warnings        
    }

    public class DataHolderAccessToken
    {
        public DataHolderAccessToken(string? clientId)
        {
            ClientId = clientId;
        }

        public string URL { get; set; } = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/token";

        public string CertificateFilename { get; set; } = BaseTest.CERTIFICATE_FILENAME;

        public string CertificatePassword { get; set; } = BaseTest.CERTIFICATE_PASSWORD;

        public string? ClientId { get; set; }

        public string ClientAssertionType { get; set; } = BaseTest.CLIENTASSERTIONTYPE;

        public string ClientRedirectURI { get; set; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;

        public string Scope { get; set; } = BaseTest.SCOPE_REGISTRATION;

        public string GrantType { get; set; } = "client_credentials";

        public async Task<HttpResponseMessage> GetAccessTokenResponseMessage()
        {
            var clientAssertion = new PrivateKeyJwt2
            {
                CertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD,
                Issuer = ClientId,
                Audience = URL
            }.Generate();

            var formFields = new List<KeyValuePair<string, string>>();

            if (GrantType != null)
            {
                formFields.Add(new KeyValuePair<string, string>("grant_type", GrantType));
            }

            if (ClientId != null)
            {
                formFields.Add(new KeyValuePair<string, string>("client_id", ClientId));
            }

            if (ClientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string, string>("client_assertion_type", ClientAssertionType));
            }

            if (clientAssertion != null)
            {
                formFields.Add(new KeyValuePair<string, string>("client_assertion", clientAssertion));
            }

            if (Scope != null)
            {
                formFields.Add(new KeyValuePair<string, string>("scope", Scope));
            }

            if (ClientRedirectURI != null)
            {
                formFields.Add(new KeyValuePair<string, string>("redirect_uri", ClientRedirectURI));
            }

            var content = new FormUrlEncodedContent(formFields);

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            clientHandler.ClientCertificates.Add(new X509Certificate2(CertificateFilename, CertificatePassword, X509KeyStorageFlags.Exportable));

            using var client = new HttpClient(clientHandler);

            BaseTest.AttachHeadersForStandAlone(URL, content.Headers);

            var response = await client.PostAsync(URL, content);

            return response;
        }

        public async Task<string> GetAccessToken(bool expired = false)
        {
            if (expired)
            {
                return BaseTest.DATAHOLDER_ACCESSTOKEN_EXPIRED;
            }

            var responseMessage = await GetAccessTokenResponseMessage();

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(DataHolderAccessToken)}.{nameof(GetAccessToken)} - Error getting access token - {responseMessage.StatusCode} - {await responseMessage.Content.ReadAsStringAsync()}");
            }

            var content = await responseMessage.Content.ReadAsStringAsync();

            var tokenResponse = JsonConvert.DeserializeObject<DataHolderTokenResponse>(content);

            return tokenResponse.AccessToken;
        }
    }
}
