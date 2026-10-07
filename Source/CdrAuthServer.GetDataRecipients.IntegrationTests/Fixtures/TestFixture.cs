using System.Threading.Tasks;
using Xunit;

namespace CdrAuthServer.GetDataRecipients.IntegrationTests.Fixtures
{
    public class TestFixture : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
