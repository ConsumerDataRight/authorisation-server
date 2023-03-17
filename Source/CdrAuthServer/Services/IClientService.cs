namespace CdrAuthServer.Services
{
    using CdrAuthServer.Models;
    using Microsoft.IdentityModel.Tokens;

    public interface IClientService
    {
        Task<Client?> Get(string? clientId);
        Task<Client> Create(Client client);
        Task<Client> Update(Client client);
        Task Delete(string clientId);
        Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> GetJwks(Client client);
        Task<IList<SecurityKey>> GetSigningKeys(Client client);
        Task<Client> GetClientBySoftwareProductId(string softwareProductId);
    }
}
