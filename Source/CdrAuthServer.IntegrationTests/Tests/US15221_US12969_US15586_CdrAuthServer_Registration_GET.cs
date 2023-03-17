using CdrAuthServer.IntegrationTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15586_CdrAuthServer_Registration_GET : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        [Fact]
        public async Task AC08_Get_WithValidClientId_ShouldRespondWith_200OK_Profile()
        {
            // Arrange
            var clientId = GetClientId(SOFTWAREPRODUCT_ID);
            var accessToken = await new DataHolderAccessToken(clientId).GetAccessToken();

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task AC09_Get_WithExpiredAccessToken_ShouldRespondWith_401Unauthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var clientId = GetClientId(SOFTWAREPRODUCT_ID);
            var accessToken = await new DataHolderAccessToken(clientId).GetAccessToken(true);

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Assert - Check WWWAutheticate header
                    Assert_HasHeader(@"Bearer error=""invalid_token"", error_description=""The token expired at '05/16/2022 03:04:03'""",
                        response.Headers, "WWW-Authenticate");
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.OK)]
        [InlineData(GUID_FOO, HttpStatusCode.Unauthorized)]
        public async Task AC10_Get_WithInvalidClientId_ShouldRespondWith_401Unauthorized_WWWAuthenticateHeader(string softwareProductId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var accessToken = await new DataHolderAccessToken(GetClientId(SOFTWAREPRODUCT_ID)).GetAccessToken();

            var clientId = softwareProductId switch
            {
                SOFTWAREPRODUCT_ID => GetClientId(softwareProductId),
                _ => softwareProductId
            };

            // Act
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId.ToLower()}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                AccessToken = accessToken
            };
            var response = await api.SendAsync(AllowAutoRedirect: false);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(expectedStatusCode);

                // Assert - Check error response
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check error response 
                    Assert_HasHeader(@"Bearer error=""invalid_request"", error_description=""The client is unknown""",
                        response.Headers,
                        "WWW-Authenticate",
                        true); // starts with
                }
            }
        }
    }
}