using CdrAuthServer.Domain.Entities;

namespace CdrAuthServer.Domain.Repositories
{
    public interface IArrangementsRepository
    {
        /// <summary>
        /// Gets the CDR Arrangement Ids for clients matching the specified software product ids.
        /// </summary>
        /// <param name="softwareProductIds">The software product ids for which to find arrangements.</param>
        /// <returns>The <c>cdr_arrangement_id</c> values for the provided <paramref name="softwareProductIds"/>.</returns>
        Task<IEnumerable<(Client Client, string ArrangementId)>> GetArrangementIdsForSoftwareProducts(IEnumerable<string> softwareProductIds);
    }
}
