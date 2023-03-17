using CdrAuthServer.Configuration;
using CdrAuthServer.Models;
using Newtonsoft.Json;

namespace CdrAuthServer.Services
{
    public class CustomerService : ICustomerService
    {        
        private readonly IConfiguration _config;
        private readonly ILogger<CustomerService> _logger;        
        private string SeedDataFilePath => _config[Keys.SeedDataFilePath];

        public CustomerService(
            IConfiguration config,
            ILogger<CustomerService> logger)
        {
            _config = config;
            _logger = logger;            
        }

        public async Task<UserInfo> Get(string subjectId)
        {
            UserInfo userInfo = new UserInfo();
            bool isUrlPath = Uri.IsWellFormedUriString(SeedDataFilePath, UriKind.Absolute);
            string customerDataJson = string.Empty;
            if (isUrlPath)
            {
                customerDataJson = await DownloadSeedDataFile(SeedDataFilePath);
            }
            else
            {
                if (!File.Exists(SeedDataFilePath))
                {
                    _logger.LogInformation("Seed data file '{SeedDataFilePath}' not found.", SeedDataFilePath);
                    return userInfo;
                }
                customerDataJson = await File.ReadAllTextAsync(SeedDataFilePath);
            }

            if (string.IsNullOrEmpty(customerDataJson))
            {
                _logger.LogInformation("Seed data is unavailable.");
                return userInfo;
            }
            
            var dataHolderCustomer = JsonConvert.DeserializeObject<DataHolderCustomer>(customerDataJson);
            var dataHolderCustomerList = dataHolderCustomer?.Customers;
            if (dataHolderCustomerList != null && dataHolderCustomerList.Any())
            {
                var customer = dataHolderCustomerList.FirstOrDefault(x => x.LoginId == subjectId);

                if (customer == null)
                {
                    _logger.LogInformation("Customer not found with id '{loginUserId}'", subjectId);
                    return userInfo;
                }

                userInfo.GivenName = customer.Person.FirstName;
                userInfo.FamilyName = customer.Person.LastName;
                userInfo.Name = $"{customer.Person.FirstName} {customer.Person.LastName}";

                return userInfo;
            }                       
            
            return userInfo;
        }

        private async Task<string> DownloadSeedDataFile(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var content = await httpClient.GetStringAsync(url);
                return content;
            }
        }
    }
}