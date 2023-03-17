namespace CdrAuthServer.Repository.Infrastructure
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;

    internal class CdrAuthServervDatabaseContextDesignTimeFactory : IDesignTimeDbContextFactory<CdrAuthServervDatabaseContext>
    {
        public CdrAuthServervDatabaseContextDesignTimeFactory()
        {
            // A parameter-less constructor is required by the EF Core CLI tools.
        }

        public CdrAuthServervDatabaseContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<CdrAuthServervDatabaseContext>()
               .UseSqlServer("foo") // connection string is only needed if using "dotnet ef database update ..." to actually run migrations from commandline
               .Options;

            return new CdrAuthServervDatabaseContext(options);
        }
    }
}
