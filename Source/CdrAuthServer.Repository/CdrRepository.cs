namespace CdrAuthServer.Repository
{
    using AutoMapper;
    using CdrAuthServer.Domain.Entities;
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Repository.Infrastructure;
    using Microsoft.EntityFrameworkCore;

    public class CdrRepository : ICdrRepository
    {
        private readonly CdrAuthServerDatabaseContext cdrAuthServervDatabaseContext;
        private readonly IMapper mapper;

        public CdrRepository(CdrAuthServerDatabaseContext cdrAuthServervDatabaseContext, IMapper mapper)
        {
            this.cdrAuthServervDatabaseContext = cdrAuthServervDatabaseContext;
            this.mapper = mapper;
        }

        public async Task<SoftwareProduct> GetSoftwareProduct(string softwareProductId)
        {
            var softwareProduct = await this.cdrAuthServervDatabaseContext.SoftwareProducts.AsNoTracking().FirstOrDefaultAsync(sp => sp.SoftwareProductId == softwareProductId);

            return mapper.Map<SoftwareProduct>(softwareProduct);
        }

        public async Task InsertDataRecipients(List<SoftwareProduct> softwareProducts)
        {                        
            var newSoftwareProductList = mapper.Map<List<CdrAuthServer.Repository.Entities.SoftwareProduct>>(softwareProducts);

            using (var transaction = cdrAuthServervDatabaseContext.Database.BeginTransaction())
            {                
                // Bulk insert the new data recipient software products.
                await cdrAuthServervDatabaseContext.SoftwareProducts.AddRangeAsync(newSoftwareProductList);
                await cdrAuthServervDatabaseContext.SaveChangesAsync();

                // Commit the transaction.
                transaction.Commit();                
            }            
        }

        public async Task PurgeDataRecipients()
        {
            using var transaction = cdrAuthServervDatabaseContext.Database.BeginTransaction();
            // Remove the existing data recipients software products.
            var existingSoftwareProducts = await cdrAuthServervDatabaseContext.SoftwareProducts.AsNoTracking().Where(sp => sp.SoftwareProductId != "cdr-register").ToListAsync();

            if (existingSoftwareProducts.Count > 0)
            {
                cdrAuthServervDatabaseContext.RemoveRange(existingSoftwareProducts);
                await cdrAuthServervDatabaseContext.SaveChangesAsync();
            }
            // Commit the transaction.
            transaction.Commit();
        }
        
    }
}