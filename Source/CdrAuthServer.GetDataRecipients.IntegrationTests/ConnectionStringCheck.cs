#nullable enable

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace CdrAuthServer.GetDataRecipients.IntegrationTests
{
    public static class ConnectionStringCheck
    {
        internal const string PRODUCTION_SERVER = "sql-cdrsandbox-prod.database.windows.net";

        // TODO - MJS - Whitelist would be better since if production database ever changes server this blacklist will fail unless someone remembers to update
        private static readonly string[] Blacklist =
        [
            PRODUCTION_SERVER,
        ];

        public static string Check(string? connectionString)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Reject if blacklisted string found in connectionString
                foreach (string blacklisted in Blacklist)
                {
                    if (connectionString.ToUpper().Trim().Contains(blacklisted.ToUpper().Trim()))
                    {
                        throw new Exception($"{blacklisted} is blacklisted. Cannot connect to this server");  // nb: don't show connectionString since it contains password
                    }
                }
            }

            return connectionString ?? string.Empty;
        }
    }

    public class ConnectionStringCheckUnitTests
    {
        private const string PRODUCTION_SERVER_FOO = "foo" + ConnectionStringCheck.PRODUCTION_SERVER + "foo"; // blacklist is checking for substrings, so surround with "foo" to ensure we are testing this

        [Theory]
        [InlineData(PRODUCTION_SERVER_FOO)]
        [InlineData(PRODUCTION_SERVER_FOO, true)]
        public void WhenOnBlackList_ShouldThrowException(string connectionString, bool? uppercase = false)
        {
            if (uppercase == true)
            {
                connectionString = connectionString.ToUpper();
            }

            using (new AssertionScope())
            {
                // Act/Assert
                Action act = () => ConnectionStringCheck.Check(connectionString);
                using (new AssertionScope())
                {
                    act.Should().Throw<Exception>();
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData("sql-cdrsandbox-dev.database.windows.net")]
        [InlineData("sql-cdrsandbox-test.database.windows.net")]
        [InlineData("localhost")]
        [InlineData("mssql")]
        public void WhenNotOnBlackList_ShouldNotThrowException(string? connectionString)
        {
            using (new AssertionScope())
            {
                // Act/Assert
                string? returnedConnectionString = null;
                Action act = () => returnedConnectionString = ConnectionStringCheck.Check(connectionString);
                using (new AssertionScope())
                {
                    act.Should().NotThrow<Exception>();
                    returnedConnectionString?.Should().Be(connectionString ?? string.Empty);
                }
            }
        }
    }
}
