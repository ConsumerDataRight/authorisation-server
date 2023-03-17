using CdrAuthServer.Models;

namespace CdrAuthServer.Services
{
    public interface ICdrService
    {
        Task<SoftwareProduct> GetSoftwareProduct(string softwareProductId);
        Task InsertDataRecipients(List<SoftwareProduct> softwareProducts);
        Task PurgeDataRecipients();
    }
}
