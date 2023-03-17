
namespace CdrAuthServer.Domain.Repositories
{
    using CdrAuthServer.Domain.Entities;

    public interface ICdrRepository
    {
        Task<SoftwareProduct> GetSoftwareProduct(string softwareProductId);
        Task InsertDataRecipients(List<SoftwareProduct> softwareProducts);
        Task PurgeDataRecipients();
    }
}
