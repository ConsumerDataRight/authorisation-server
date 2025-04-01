#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

#nullable enable

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using CdrAuthServer.GetDataRecipients.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Sdk;

namespace CdrAuthServer.GetDataRecipients.IntegrationTests
{
    internal class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
    {
        private static int count = 0;

        public override void Before(MethodInfo methodUnderTest)
        {
            Console.WriteLine($"Test #{++count} - {methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }
}
