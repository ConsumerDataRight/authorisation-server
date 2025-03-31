using Xunit;
using Xunit.Sdk;

namespace XUnit_Skippable
{
    [XunitTestCaseDiscoverer("CdrAuthServer.IntegrationTests.XUnit_Skippable.SkippableFactDiscoverer", "CdrAuthServer.IntegrationTests")]
    public class SkippableFactAttribute : FactAttribute
    {
    }
}
