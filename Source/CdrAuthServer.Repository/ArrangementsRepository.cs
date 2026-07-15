namespace CdrAuthServer.Repository
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AutoMapper;
    using CdrAuthServer.Domain;
    using CdrAuthServer.Domain.Entities;
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Repository.Infrastructure;
    using Microsoft.EntityFrameworkCore;

    public class ArrangementsRepository(CdrAuthServerDatabaseContext dbContext, IMapper mapper) : IArrangementsRepository
    {
        /// <inheritdoc />
        public async Task<IEnumerable<(Client, string)>> GetArrangementIdsForSoftwareProducts(IEnumerable<string> softwareProductIds)
        {
            var arrangements = await dbContext.ClientClaims
                .Where(claims => claims.Type == Constants.ClaimNames.SoftwareId)
                .Where(claim => softwareProductIds.Contains(claim.Value))
                .Join(
                    dbContext.Grants.Where(grant => grant.GrantType == Constants.GrantTypes.CdrArrangement),
                    claim => claim.ClientId,
                    grant => grant.ClientId,
                    (claim, grant) => new { claim.Client, ArrangementId = grant.Key, RecipientBaseUri = claim.Client.ClientClaims!.OrderBy(x => x.Id).Last(x => x.Type == Constants.ClaimNames.RecipientBaseUri) }) // The grant key is the cdr_arrangement_id
                .ToArrayAsync();

            return arrangements.Select(x =>
            {
                var client = mapper.Map<Client>(x.Client);
                client.RecipientBaseUri = x.RecipientBaseUri.Value;
                return (client, x.ArrangementId);
            });
        }
    }
}
