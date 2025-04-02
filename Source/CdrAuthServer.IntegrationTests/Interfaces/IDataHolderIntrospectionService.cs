using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using System.Net.Http;
using System.Threading.Tasks;

namespace CdrAuthServer.IntegrationTests.Interfaces
{
    public interface IDataHolderIntrospectionService
    {
        Task<HttpResponseMessage> SendRequest(string? grantType = "client_credentials", string? clientId = null, string? clientAssertionType = Constants.ClientAssertionType, string? clientAssertion = null, string? token = null, string? tokenTypeHint = "refresh_token");
    }
}
