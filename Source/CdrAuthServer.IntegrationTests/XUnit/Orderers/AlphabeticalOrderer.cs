﻿using Xunit.Abstractions;
using Xunit.Sdk;

namespace CdrAuthServer.IntegrationTests.XUnit.Orderers
{
    public class AlphabeticalOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
            =>
            testCases.OrderBy(testCase => testCase.TestMethod.Method.Name);
    }
}
