namespace CdrAuthServer.Repository.Configuration
{
    using System.Text.Json;
    using CdrAuthServer.Domain;
    using CdrAuthServer.Repository.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using static CdrAuthServer.Domain.Constants;

    internal class GrantConfiguration : IEntityTypeConfiguration<Grant>
    {
        public void Configure(EntityTypeBuilder<Grant> builder)
        {
            builder.HasKey(x => x.Key);

            const string refreshToken = "valid-refresh-token";
            var cdrArrangementGrant = new Grant()
            {
                Key = "12345678-1234-1234-1234-111122223333",
                ClientId = "c6327f87-687a-4369-99a4-eaacd3bb8210",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(365),
                GrantType = GrantTypes.CdrArrangement,
                SubjectId = "customer1",
                UsedAt = null,
                Scope = Scopes.AllSectorScopes,
                Data = JsonSerializer.Serialize(
                    new Dictionary<string, object>
                    {
                        { "refresh_token", refreshToken },
                        { "account_id", new List<string> {"123", "456", "789" } }
                    })
            };
            builder.HasData(
                    cdrArrangementGrant,

                    //RefreshTokenGrant
                    new Grant()
                    {
                        Key = refreshToken,
                        ClientId = "c6327f87-687a-4369-99a4-eaacd3bb8210",
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(365),
                        GrantType = GrantTypes.RefreshToken,
                        SubjectId = "customer1",
                        UsedAt = null,
                        Scope = Scopes.AllSectorScopes,
                        Data = JsonSerializer.Serialize(new Dictionary<string, object> {
                        { "response_type", ResponseTypes.Hybrid },
                        { "CdrArrangementId", cdrArrangementGrant.Key }
                        })
                    },

                    //RefreshTokenGrant2
                    new Grant()
                    {
                        Key = "expired-refresh-token",
                        ClientId = "c6327f87-687a-4369-99a4-eaacd3bb8210",
                        CreatedAt = DateTime.UtcNow.AddDays(-366),
                        ExpiresAt = DateTime.UtcNow.AddDays(-1),
                        GrantType = GrantTypes.RefreshToken,
                        SubjectId = "customer1",
                        UsedAt = null,
                        Scope = Scopes.BankingSectorScopes,
                        Data = JsonSerializer.Serialize(new Dictionary<string, object> {
                        { "response_type", ResponseTypes.Hybrid },
                        { "CdrArrangementId", Guid.NewGuid().ToString() }
                        })
                    });
        }
    }
}
