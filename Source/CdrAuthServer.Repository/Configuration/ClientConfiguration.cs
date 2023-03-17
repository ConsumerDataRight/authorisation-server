namespace CdrAuthServer.Repository.Configuration
{
    using CdrAuthServer.Repository.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.HasKey(x => x.ClientId);

            builder.HasMany(c => c.ClientClaims).WithOne(cc => cc.Client).HasForeignKey(c => c.ClientId).IsRequired().OnDelete(DeleteBehavior.Cascade);

            //seed client
            builder.HasData(
                 new Client()
                 {
                     ClientId = "11111111-1111-1111-1111-111111111111",
                     ClientName = "Software Product 1",
                 });
        }
    }
}
