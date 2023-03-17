namespace CdrAuthServer.Repository
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AutoMapper;
    using CdrAuthServer.Domain.Entities;
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Repository.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    

    public class GrantRepository : IGrantRepository
    {

        private readonly CdrAuthServervDatabaseContext cdrAuthServervDatabaseContext;
        private readonly IMapper mapper;
        private readonly ILogger<GrantRepository> logger;

        public GrantRepository(CdrAuthServervDatabaseContext cdrAuthServervDatabaseContext, IMapper mapper, ILogger<GrantRepository> logger)
        {
            this.cdrAuthServervDatabaseContext = cdrAuthServervDatabaseContext;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<Grant> Create(Grant grant)
        {
            try
            {
                var grantEntity = this.mapper.Map<Entities.Grant>(grant);

                await cdrAuthServervDatabaseContext.Grants.AddAsync(grantEntity);
                cdrAuthServervDatabaseContext.SaveChanges();

                grant.Key = grantEntity.Key.ToString();

                return grant;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Create Grant failed for {@Grant}", grant);
                throw;
            }
        }

        public async Task Delete(string key)
        {
            var grant = await cdrAuthServervDatabaseContext.Grants.FirstOrDefaultAsync(g => g.Key == key);

            if (grant != null)
            {
                try
                {
                    cdrAuthServervDatabaseContext.Grants.Remove(grant);
                    await cdrAuthServervDatabaseContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Delete Graint failed for {@Grant}", grant);
                }
            }
        }

        public async Task<Grant> Get(string key)
        {
            var grant = await cdrAuthServervDatabaseContext.Grants.AsNoTracking().FirstOrDefaultAsync(g => g.Key == key);
            return mapper.Map<Grant>(grant);
        }

        public async Task<IList<Grant>> List(string clientId, string grantType)
        {
            var grants = await cdrAuthServervDatabaseContext.Grants.AsNoTracking().ToListAsync();
            return mapper.Map<IList<Grant>>(grants);
        }

        public async Task<Grant?> Update(Grant grant)
        {
            var _grant = await cdrAuthServervDatabaseContext.Grants.AsNoTracking().FirstOrDefaultAsync(g => g.Key == grant.Key);
            
            if (_grant == null)
            {
                return null;
            }

            try
            {
                var grantToUpdate = mapper.Map<Entities.Grant>(grant);                
                cdrAuthServervDatabaseContext.Grants.Update(grantToUpdate);
                await cdrAuthServervDatabaseContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Update Graint failed for {@Grant}", grant);
                return null;
            }

            return grant;
        }
    }
}
