using System;
using System.Threading.Tasks;

#nullable enable

namespace CdrAuthServer.IntegrationTests.Infrastructure.API2
{
    public class EDataHolder_Authorise_IncorrectCustomerId : Exception { }
    public class EDataHolder_Authorise_IncorrectOneTimePassword : Exception { }

    public class DataHolder_Authorise_APIv2
    {
        /// <summary>
        /// The customer's userid with the DataHolder - eg "jwilson"
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// The OTP (One-time password) that is sent to the customer (via sms etc) so the DataHolder can authenticate the Customer.
        /// For the mock solution use "000789"
        /// </summary>
        public string? OTP { get; init; }

        /// <summary>
        /// Comma delimited list of account ids the user is granting consent for
        /// </summary>
        public string? SelectedAccountIds { get; init; }

        /// <summary>
        /// Scope
        /// </summary>
        public string Scope { get; init; } = BaseTest.SCOPE;

        /// <summary>
        /// Lifetime (in seconds) of the access token
        /// </summary>
        public int TokenLifetime { get; init; } = 3600;

        /// <summary>
        /// Lifetime (in seconds) of the CDR arrangement.
        /// SHARING_DURATION = 90 days
        /// </summary>
        public int? SharingDuration { get; init; } = BaseTest.SHARING_DURATION;

        public string? RequestUri { get; init; }

        public string CertificateFilename { get; init; } = BaseTest.CERTIFICATE_FILENAME;
        public string CertificatePassword { get; init; } = BaseTest.CERTIFICATE_PASSWORD;
        public string ClientId { get; init; } = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID).ToLower();
        public string RedirectURI { get; init; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
        public string JwtCertificateFilename { get; init; } = BaseTest.JWT_CERTIFICATE_FILENAME;
        public string JwtCertificatePassword { get; init; } = BaseTest.JWT_CERTIFICATE_PASSWORD;

        public string ResponseType { get; init; } = "code id_token";

        /// <summary>
        /// Perform authorisation and consent flow. Returns authCode and idToken
        /// </summary>
        public async Task<(string authCode, string idToken)> Authorise(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS)
        {
            if (BaseTest.HEADLESSMODE)
            {
                return await new DataHolder_Authorise_APIv2_Headless
                {
                    UserId = UserId,
                    OTP = OTP,
                    SelectedAccountIds = SelectedAccountIds,
                    Scope = Scope,
                    TokenLifetime = TokenLifetime,
                    SharingDuration = SharingDuration,
                    RequestUri = RequestUri,
                    CertificateFilename = CertificateFilename,
                    CertificatePassword = CertificatePassword,
                    ClientId = ClientId,
                    RedirectURI = RedirectURI,
                    JwtCertificateFilename = JwtCertificateFilename,
                    JwtCertificatePassword = JwtCertificatePassword,
                    ResponseType = ResponseType
                }.Authorise(redirectUrl: redirectUrl);
            }
            else
            {
                return await new DataHolder_Authorise_APIv2_NonHeadless
                {
                    UserId = UserId,
                    OTP = OTP,
                    SelectedAccountIds = SelectedAccountIds,
                    Scope = Scope,
                    TokenLifetime = TokenLifetime,
                    SharingDuration = SharingDuration,
                    RequestUri = RequestUri,
                    CertificateFilename = CertificateFilename,
                    CertificatePassword = CertificatePassword,
                    ClientId = ClientId,
                    RedirectURI = RedirectURI,
                    JwtCertificateFilename = JwtCertificateFilename,
                    JwtCertificatePassword = JwtCertificatePassword,
                    ResponseType = ResponseType
                }.Authorise(redirectUrl: redirectUrl);
            }
        }
    }

    public abstract class DataHolder_Authorise_APIv2_Base
    {
        public string? UserId { get; init; }
        public string? OTP { get; init; }
        public string? SelectedAccountIds { get; init; }
        protected string[]? SelectedAccountIdsArray => SelectedAccountIds?.Split(",");
        public string Scope { get; init; } = BaseTest.SCOPE;
        public int TokenLifetime { get; init; } = 3600;
        public int? SharingDuration { get; init; } = BaseTest.SHARING_DURATION;
        public string? RequestUri { get; init; }
        public string CertificateFilename { get; init; } = BaseTest.CERTIFICATE_FILENAME;
        public string CertificatePassword { get; init; } = BaseTest.CERTIFICATE_PASSWORD;
        public string ClientId { get; init; } = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID).ToLower();
        public string RedirectURI { get; init; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
        public string JwtCertificateFilename { get; init; } = BaseTest.JWT_CERTIFICATE_FILENAME;
        public string JwtCertificatePassword { get; init; } = BaseTest.JWT_CERTIFICATE_PASSWORD;
        public string ResponseType { get; init; } = "code id_token";
        public abstract Task<(string authCode, string idToken)> Authorise(string redirectUrl = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
    }
}
