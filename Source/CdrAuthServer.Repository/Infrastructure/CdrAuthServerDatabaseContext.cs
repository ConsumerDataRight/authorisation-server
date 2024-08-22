namespace CdrAuthServer.Repository.Infrastructure
{
    using CdrAuthServer.Repository.Configuration;
    using CdrAuthServer.Repository.Entities;
    using Microsoft.EntityFrameworkCore;

    public class CdrAuthServerDatabaseContext : DbContext
    {
        public CdrAuthServerDatabaseContext()
        {
        }

        public CdrAuthServerDatabaseContext(DbContextOptions<CdrAuthServerDatabaseContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }

        public DbSet<ClientClaims> ClientClaims { get; set; }

        public DbSet<Grant> Grants { get; set; }

        public DbSet<SoftwareProduct> SoftwareProducts { get; set; }

        public DbSet<Token> Tokens { get; set; }

        public DbSet<LogEventsDrService> LogEventsDrService { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ClientConfiguration());
            modelBuilder.ApplyConfiguration(new ClientClaimsConfiguration());
            modelBuilder.ApplyConfiguration(new SoftwareProductConfiguration());
            modelBuilder.ApplyConfiguration(new GrantConfiguration());
            modelBuilder.ApplyConfiguration(new TokenConfiguration());
        }
    }
}
