namespace CdrAuthServer.Repository
{
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Repository.Infrastructure;
    using Microsoft.EntityFrameworkCore;

    public class TokenRepository : ITokenRepository
    {
        private readonly CdrAuthServervDatabaseContext cdrAuthServervDatabaseContext;

        public TokenRepository(CdrAuthServervDatabaseContext cdrAuthServervDatabaseContext)
        {
            this.cdrAuthServervDatabaseContext = cdrAuthServervDatabaseContext;
        }

        public async Task AddToBlacklist(string id)
        {
            await cdrAuthServervDatabaseContext.Tokens.AddAsync(
                new Entities.Token
                {
                    Id = id,
                    BlackListed = true
                });

            await cdrAuthServervDatabaseContext.SaveChangesAsync();
        }

        public async Task<bool> IsTokenBlacklisted(string id)
        {
            var token = await cdrAuthServervDatabaseContext.Tokens.FirstOrDefaultAsync(t => t.Id == id);

            return token?.BlackListed ?? false;
        }
    }
}