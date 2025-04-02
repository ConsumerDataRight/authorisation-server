using Xunit;
using Xunit.Sdk;

namespace XUnit_Skippable
{
    [XunitTestCaseDiscoverer("CdrAuthServer.IntegrationTests.XUnit_Skippable.SkippableTheoryDiscoverer", "CdrAuthServer.IntegrationTests")]
    public class SkippableTheoryAttribute : TheoryAttribute
    {
    }
}
