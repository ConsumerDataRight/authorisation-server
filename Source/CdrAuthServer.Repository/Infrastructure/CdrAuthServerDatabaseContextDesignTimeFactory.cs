namespace CdrAuthServer.Repository.Infrastructure
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;

    internal class CdrAuthServerDatabaseContextDesignTimeFactory : IDesignTimeDbContextFactory<CdrAuthServerDatabaseContext>
    {
        public CdrAuthServerDatabaseContextDesignTimeFactory()
        {
            // A parameter-less constructor is required by the EF Core CLI tools.
        }

        public CdrAuthServerDatabaseContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<CdrAuthServerDatabaseContext>()
               .UseSqlServer("foo") // connection string is only needed if using "dotnet ef database update ..." to actually run migrations from commandline
               .Options;

            return new CdrAuthServerDatabaseContext(options);
        }
    }
}
