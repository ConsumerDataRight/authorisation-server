using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class DataHolderAccessTokenResponse
    {
        static public async Task<DataHolderAccessTokenResponse> Deserialize(HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<DataHolderAccessTokenResponse>(content);
        }

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
    }

    public class DataHolderAccessToken
    {
        private readonly string _dhMtlsGatewayUrl;
        private readonly string _redirectUri;
        private readonly string _xtlsThumbprint;
        private readonly bool _isStandalone;

        public DataHolderAccessToken(string? clientId, string dhMtlsGatewayUrl, string redirectUri, string xtlsThumbprint, bool isStandalone)
        {
            ClientId = clientId;
            _dhMtlsGatewayUrl = dhMtlsGatewayUrl;
            _redirectUri = redirectUri;
            _xtlsThumbprint = xtlsThumbprint;
            _isStandalone = isStandalone;
            URL = $"{_dhMtlsGatewayUrl}/connect/token";
            ClientRedirectURI = _redirectUri;
        }

        public string URL { get; init; }

        public string CertificateFilename { get; set; } = Constants.Certificates.CertificateFilename;

        public string CertificatePassword { get; set; } = Constants.Certificates.CertificatePassword;

        public string? ClientId { get; set; }

        public string ClientAssertionType { get; set; } = Constants.ClientAssertionType;

        public string ClientRedirectURI { get; init; }

        public string Scope { get; set; } = Constants.Scopes.ScopeRegistration;

        public string GrantType { get; set; } = "client_credentials";

        public async Task<HttpResponseMessage> GetAccessTokenResponseMessage()
        {
            var clientAssertion = new PrivateKeyJwtService
            {
                CertificateFilename = Constants.Certificates.JwtCertificateFilename,
                CertificatePassword = Constants.Certificates.JwtCertificatePassword,
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

            using var client = Helpers.Web.CreateHttpClient(CertificateFilename, CertificatePassword);

            Helpers.AuthServer.AttachHeadersForStandAlone(URL, content.Headers, _dhMtlsGatewayUrl, _xtlsThumbprint, _isStandalone);

            var response = await client.PostAsync(URL, content);

            return response;
        }

        public async Task<string> GetAccessToken(bool expired = false)
        {
            if (expired)
            {
                return Constants.AccessTokens.DataHolderAccessTokenExpired;
            }

            var responseMessage = await GetAccessTokenResponseMessage();

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{nameof(DataHolderAccessToken)}.{nameof(GetAccessToken)} - Error getting access token - {responseMessage.StatusCode} - {await responseMessage.Content.ReadAsStringAsync()}");
            }

            var content = await responseMessage.Content.ReadAsStringAsync();

            var tokenResponse = JsonConvert.DeserializeObject<DataHolderAccessTokenResponse>(content);

            return tokenResponse.AccessToken;
        }
    }
}
