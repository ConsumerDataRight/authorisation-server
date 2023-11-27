using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Dataholders;
using System.Threading.Tasks;

namespace CdrAuthServer.IntegrationTests.Interfaces
{
    public interface IAuthorizationService
    {
        Task<TokenResponse> GetToken(TokenType tokenType, string? clientId = null, int tokenLifetime = Constants.AuthServer.DefaultTokenLifetime, int sharingDuration = Constants.AuthServer.SharingDuration);
    }
}