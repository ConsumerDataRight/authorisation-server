#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

#nullable enable

using System;
using System.Configuration;
using System.IO;
using CdrAuthServer.GetDataRecipients.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CdrAuthServer.GetDataRecipients.IntegrationTests
{
    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("IntegrationTests")]
    [TestCaseOrderer("CdrAuthServer.GetDataRecipients.IntegrationTests.XUnit.Orderers.AlphabeticalOrderer", "CdrAuthServer.GetDataRecipients.IntegrationTests")]
    [DisplayTestMethodName]
    public abstract class BaseTest : IClassFixture<TestFixture>
    {
        public static string AZUREFUNCTIONS_URL => Configuration["URL:AZUREFUNCTIONS"]
            ?? throw new ConfigurationErrorsException($"{nameof(AZUREFUNCTIONS_URL)} - configuration setting not found");

        public static string CONNECTIONSTRING_REGISTER_RW =>
            ConnectionStringCheck.Check(Configuration.GetConnectionString("Register_RW"));

        public static string CONNECTIONSTRING_AUTHSERVER_RW =>
            ConnectionStringCheck.Check(Configuration.GetConnectionString("CdrAuthServer_DB_RW"));

        private static IConfigurationRoot? configuration;

        public static IConfigurationRoot Configuration
        {
            get
            {
                configuration ??= new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .Build();

                return configuration;
            }
        }
    }
}
