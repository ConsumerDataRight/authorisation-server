
#if INTEGRATION_TESTS
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace CdrAuthServer.GetDataRecipients
{
    public class GetDataRecipients_IntegrationTestsHelper
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<GetDROptions> _options;

        public GetDataRecipients_IntegrationTestsHelper(ILoggerFactory loggerFactory, IOptions<GetDROptions> options)
        {
            _loggerFactory = loggerFactory;
            _options=options;
            _logger = loggerFactory.CreateLogger<GetDataRecipients_IntegrationTestsHelper>();
            
        }
        // This http trigger is used the integration tests so that DATARECIPIENTS can be triggered on demand and not wait for timer
        [Function("INTEGRATIONTESTS_DATARECIPIENTS")]
        public async Task<IActionResult> INTEGRATIONTESTS_DATARECIPIENTS(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
             _logger.LogInformation($"{nameof(GetDataRecipients_IntegrationTestsHelper)}.{nameof(INTEGRATIONTESTS_DATARECIPIENTS)}");

             // Call the actual Azure function
             GetDataRecipientsFunction getDataRecipientsFunction = new GetDataRecipientsFunction(_loggerFactory, _options); 
             await getDataRecipientsFunction.Run(null);            

            return new OkResult();            
        }
    }
}
#endif