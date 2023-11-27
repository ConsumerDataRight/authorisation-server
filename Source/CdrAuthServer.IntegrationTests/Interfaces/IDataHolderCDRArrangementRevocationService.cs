using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using System.Net.Http;
using System.Threading.Tasks;

namespace CdrAuthServer.IntegrationTests.Interfaces
{
    public interface IDataHolderCDRArrangementRevocationService
    {
        Task<HttpResponseMessage> SendRequest(string? grantType = "client_credentials", string? clientId = null, string? clientAssertionType = Constants.ClientAssertionType, string? cdrArrangementId = null, string? clientAssertion = null, string? certificateFilename = null, string? certificatePassword = null, string? jwtCertificateFilename = Constants.Certificates.JwtCertificateFilename, string? jwtCertificatePassword = Constants.Certificates.JwtCertificatePassword);
    }
}