namespace CdrAuthServer.Domain.Repositories
{
    using CdrAuthServer.Domain.Entities;

    public interface IClientRepository
    {
        Task<Client?> Get(string clientId);
        Task<Client?> GetBySoftwareProductId(string softwareProductId);
        Task<Client> Create(Client client);
        Task<Client?> Update(Client client);
        Task<bool> Delete(string clientId);
    }
}
