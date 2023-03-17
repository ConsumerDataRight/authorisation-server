namespace CdrAuthServer.Repository
{
    using System.Threading.Tasks;
    using AutoMapper;
    using CdrAuthServer.Domain;
    using CdrAuthServer.Domain.Entities;
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Repository.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class ClientRepository : IClientRepository
    {
        private readonly CdrAuthServervDatabaseContext cdrAuthServervDatabaseContext;
        private readonly IMapper mapper;
        private readonly ILogger<ClientRepository> logger;

        public ClientRepository(CdrAuthServervDatabaseContext cdrAuthServervDatabaseContext, IMapper mapper, ILogger<ClientRepository> logger)
        {
            this.cdrAuthServervDatabaseContext = cdrAuthServervDatabaseContext;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<Client> Create(Client client)
        {
            try
            {
                var clientEntity = mapper.Map<Entities.Client>(client);

                await cdrAuthServervDatabaseContext.Clients.AddAsync(clientEntity);
                cdrAuthServervDatabaseContext.SaveChanges();

                client.ClientId = clientEntity.ClientId.ToString();

                return client;

            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "Create Client failed for {@Client}", client);
                throw;
            }
        }

        public async Task<bool> Delete(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return false;
            }

            var client = await cdrAuthServervDatabaseContext.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            if (client != null)
            {
                try
                {
                    cdrAuthServervDatabaseContext.Clients.Remove(client);
                    var saveResult = await cdrAuthServervDatabaseContext.SaveChangesAsync();

                    return saveResult > 0;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Delete Client failed for {@Client}", client);
                    return false;
                }
            }

            return false;
        }

        public async Task<Client?> Get(string clientId)
        {
            var client = await cdrAuthServervDatabaseContext.Clients.AsNoTracking().Include(client => client.ClientClaims)
                .FirstOrDefaultAsync(client => client.ClientId == clientId);

            return mapper.Map<Client>(client);
        }

        public async Task<Client?> GetBySoftwareProductId(string softwareProductId)
        {
            var clientClaims = await cdrAuthServervDatabaseContext.ClientClaims.AsNoTracking().Include(cc => cc.Client).FirstOrDefaultAsync(cc => cc.Type == Constants.ClaimNames.SoftwareId && cc.Value == softwareProductId);
            
            return mapper.Map<Client>(clientClaims?.Client);
        }

        public async Task<Client?> Update(Client client)
        {
            var entity = await cdrAuthServervDatabaseContext.Clients.FindAsync(client.ClientId);
            if (entity == null)
            {
                return null;
            }

            try
            {
                //convert the domain to entity
                var domainToEntityClient = mapper.Map<Entities.Client>(client);

                //now map entity to entity
                mapper.Map(domainToEntityClient, entity);
                
                //now save the changes.
                await cdrAuthServervDatabaseContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Update failed for {@Client}", client);
                return null;
            }

            return mapper.Map<Client>(entity);
        }
    }
}
