using CdrAuthServer.IntegrationTests.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using Microsoft.Extensions.Options;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;


namespace CdrAuthServer.IntegrationTests.Services
{
    public class DataHolderCDRArrangementRevocationService : IDataHolderCDRArrangementRevocationService
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly ISqlQueryService _sqlQueryService;

        public DataHolderCDRArrangementRevocationService(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, ISqlQueryService sqlQueryService)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
        }

        public async Task<HttpResponseMessage> SendRequest(
             string? grantType = "client_credentials",
             string? clientId = null,
             string? clientAssertionType = Constants.ClientAssertionType,
             string? cdrArrangementId = null,
             string? clientAssertion = null,
             string? certificateFilename = null,
             string? certificatePassword = null,
             string? jwtCertificateFilename = Constants.Certificates.JwtCertificateFilename,
             string? jwtCertificatePassword = Constants.Certificates.JwtCertificatePassword)
        {
            if (clientId == null)
            {
                clientId = _options.LastRegisteredClientId;
            }

            var URL = $"{_options.DH_MTLS_GATEWAY_URL}/connect/arrangements/revoke";

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
            if (cdrArrangementId != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("cdr_arrangement_id", cdrArrangementId));
            }
            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ??
                new PrivateKeyJwtService()
                {
                    CertificateFilename = jwtCertificateFilename,
                    CertificatePassword = jwtCertificatePassword,
                    Issuer = clientId ?? throw new NullReferenceException(nameof(clientId)),
                    Audience = URL
                }.Generate()
            ));
            var content = new FormUrlEncodedContent(formFields);

            using var client = Helpers.Web.CreateHttpClient(certificateFilename ?? Constants.Certificates.CertificateFilename, certificatePassword ?? Constants.Certificates.CertificatePassword);

            Helpers.AuthServer.AttachHeadersForStandAlone(URL, content.Headers, _options.DH_MTLS_GATEWAY_URL, _authServerOptions.XTLSCLIENTCERTTHUMBPRINT, _authServerOptions.STANDALONE);

            var responseMessage = await client.PostAsync(URL, content);

            return responseMessage;
        }
    }
}
