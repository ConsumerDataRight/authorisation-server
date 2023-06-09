using Xunit;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using CdrAuthServer.IntegrationTests.Fixtures;

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15587_CdrAuthServer_Registration_DELETE : BaseTest, IClassFixture<TestFixture>
    {
        // Purge database, register product and return SSA JWT and registration json
        static private async Task<(string ssa, string registration, string clientId)> Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer();
            return await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        [Fact]
        public async Task AC15_Delete_WithValidClientId_ShouldRespondWith_204NoContent_ProfileIsDeleted()
        {
            // Arrange
            var (_, _, clientId) = await Arrange();

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Delete,
                AccessToken = await new DataHolderAccessToken(clientId).GetAccessToken(),
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                await Assert_HasNoContent2(response.Content);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    // do a get, should fail
                }
            }
        }

        [Fact]
        public async Task AC17_Delete_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var (_, _, clientId) = await Arrange();

            var accessToken = await new DataHolderAccessToken(clientId).GetAccessToken(true);

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Delete,
                AccessToken = accessToken,
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at '05/16/2022 03:04:03'""",
                        response.Headers, "WWW-Authenticate");
                }
            }
        }
    }
}