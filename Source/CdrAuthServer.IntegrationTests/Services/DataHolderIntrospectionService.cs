using CdrAuthServer.IntegrationTests.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests.Services
{
    public class DataHolderIntrospectionService : IDataHolderIntrospectionService
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly ISqlQueryService _sqlQueryService;

        public DataHolderIntrospectionService(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, ISqlQueryService sqlQueryService)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
        }

        public class Response
        {
            [JsonProperty("active")]
            public bool? Active { get; set; }

            [JsonProperty("scope")]
            public string? Scope { get; set; }

            [JsonProperty("exp")]
            public int? Exp { get; set; }

            [JsonProperty("cdr_arrangement_id")]
            public string? CdrArrangementId { get; set; }
        };

        public async Task<HttpResponseMessage> SendRequest(
             string? grantType = "client_credentials",
             string? clientId = null,
             string? clientAssertionType = Constants.ClientAssertionType,
             string? clientAssertion = null,
             string? token = null,
             string? tokenTypeHint = "refresh_token")
        {
            if (clientId == null)
            {
                clientId = _options.LastRegisteredClientId;
            }

            var URL = $"{_options.DH_MTLS_GATEWAY_URL}/connect/introspect";

            var formFields = new List<KeyValuePair<string?, string?>>();
            if (grantType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("grant_type", grantType));
            }
            if (clientId != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_id", clientId.ToLower()));
            }
            if (clientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion_type", clientAssertionType));
            }
            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ??
                new PrivateKeyJwtService()
                {
                    CertificateFilename = Constants.Certificates.JwtCertificateFilename,
                    CertificatePassword = Constants.Certificates.JwtCertificatePassword,
                    Issuer = clientId ?? throw new NullReferenceException(nameof(clientId)),
                    Audience = URL
                }.Generate()
            ));
            if (token != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("token", token));
            }
            if (tokenTypeHint != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("token_type_hint", tokenTypeHint));
            }
            var content = new FormUrlEncodedContent(formFields);

            using var client = Helpers.Web.CreateHttpClient(Constants.Certificates.CertificateFilename, Constants.Certificates.CertificatePassword);

            Helpers.AuthServer.AttachHeadersForStandAlone(URL, content.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var responseMessage = await client.PostAsync(URL, content);

            return responseMessage;
        }

        static public async Task<Response?> DeserializeResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Response>(responseContent);
        }
    }
}
