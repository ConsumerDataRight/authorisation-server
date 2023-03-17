using Xunit;
using Xunit.Sdk;

namespace XUnit_Skippable
{
    [XunitTestCaseDiscoverer("XUnit_Skippable.SkippableFactDiscoverer", "CdrAuthServer.IntegrationTests")]
    public class SkippableFactAttribute : FactAttribute { }
}
