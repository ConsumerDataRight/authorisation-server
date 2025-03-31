namespace CdrAuthServer.Repository.Configuration
{
    using CdrAuthServer.Repository.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal class ClientClaimsConfiguration : IEntityTypeConfiguration<ClientClaims>
    {
        public void Configure(EntityTypeBuilder<ClientClaims> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(entity => entity.ClientId).IsRequired();

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.HasIndex(cc => cc.ClientId, "IX_ClientClaims_ClientId");

            builder.HasData(
                    new ClientClaims { Id = 1, Type = "SoftwareId", Value = "22222222-2222-2222-2222-222222222222", ClientId = "11111111-1111-1111-1111-111111111111" },
                    new ClientClaims { Id = 2, Type = "JwksUri", Value = "https://localhost:9001/jwks", ClientId = "11111111-1111-1111-1111-111111111111" });
        }
    }
}
