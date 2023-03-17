namespace CdrAuthServer.Domain.Repositories
{
    using CdrAuthServer.Domain.Entities;

    public interface IGrantRepository
    {
        Task<IList<Grant>> List(string clientId, string grantType);

        Task<Grant> Get(string key);

        Task<Grant> Create(Grant grant);

        Task<Grant?> Update(Grant grant);

        Task Delete(string key);
    }
}
