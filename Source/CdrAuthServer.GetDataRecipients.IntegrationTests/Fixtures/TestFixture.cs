using System.Threading.Tasks;
using Xunit;

namespace CdrAuthServer.GetDataRecipients.IntegrationTests.Fixtures
{
    public class TestFixture : IAsyncLifetime
    {
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}