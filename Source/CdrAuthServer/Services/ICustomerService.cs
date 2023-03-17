using CdrAuthServer.Models;

namespace CdrAuthServer.Services
{
    public interface ICustomerService
    {
        Task<UserInfo> Get(string subjectId);
    }
}