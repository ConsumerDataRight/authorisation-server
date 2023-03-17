using CdrAuthServer.Models;

namespace CdrAuthServer.Services
{
    public interface IGrantService
    {
        Task<IList<Grant>> ListForClient(string clientId, string grantType);
        Task<Grant?> Get(string grantType, string key, string? clientId = null);
        Task<Grant> Create(Grant grant);
        Task<Grant> Update(Grant grant);
        Task Delete(string? clientId, string grantType, string key);
    }
}
