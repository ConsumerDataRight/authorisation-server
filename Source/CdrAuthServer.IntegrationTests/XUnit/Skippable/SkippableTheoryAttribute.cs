using Xunit;
using Xunit.Sdk;

namespace XUnit_Skippable
{
    [XunitTestCaseDiscoverer("XUnit_Skippable.SkippableTheoryDiscoverer", "CdrAuthServer.IntegrationTests")]
    public class SkippableTheoryAttribute : TheoryAttribute { }
}
