using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using CdrAuthServer.IntegrationTests.Fixtures;
using Newtonsoft.Json;

#nullable enable

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15585_CdrAuthServer_Registration_PUT : BaseTest, IClassFixture<TestFixture>
    {
        // Purge database, register product and return SSA JWT / registration json / clientId of registered software product
        static private async Task<(string ssa, string registration, string clientId)> Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer();
            return await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        [Fact]
        public async Task AC11_Put_WithValidSoftwareProduct_ShouldRespondWith_200OK_UpdatedProfile()
        {
            // Arrange
            var (ssa, expectedResponse, clientId) = await Arrange();

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(
                ssa,
                responseType: "code,code id_token",
                authorization_signed_response_alg: "PS256",
                authorization_encrypted_response_alg: "RSA-OAEP",
                authorization_encrypted_response_enc: "A128CBC-HS256"
                );

            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = await new DataHolderAccessToken(clientId).GetAccessToken(),
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    // Assert - Check json
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC12_Put_WithInvalidSoftwareProduct_ShouldRespondWith_400BadRequest_InvalidErrorResponse()
        {
            // Arrange
            var (ssa, _, clientId) = await Arrange();

            // Act
            const string REDIRECT_URI = "foo";
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa,
                redirect_uris: new string[] { REDIRECT_URI });  // Invalid redirect uris

            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = await new DataHolderAccessToken(clientId).GetAccessToken(),
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
            };
            var response = await api.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check statuscode
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var expectedResponse = @$"{{
                        ""error"": ""invalid_redirect_uri"",
                         ""error_description"": ""ERR-DCR-003: The redirect_uri '{REDIRECT_URI}' is not valid as it is not included in the software_statement""
                    }}";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        public async Task AC13_Put_WithExpiredAccessToken_ShouldRespondWith_401UnAuthorized_ExpiredAccessTokenErrorResponse()
        {
            // Arrange
            var (ssa, _, clientId) = await Arrange();

            var accessToken = await new DataHolderAccessToken(clientId).GetAccessToken(true);

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);
            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{clientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,
                AccessToken = accessToken,
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
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

        [Theory]
        [InlineData(GUID_FOO)]
        public async Task AC14_Put_WithInvalidOrUnregisteredClientID_ShouldRespondWith_401Unauthorised_InvalidErrorResponse(string invalidClientId)
        {
            // Arrange
            var (ssa, _, clientId) = await Arrange();

            // Act
            var registrationRequest = DataHolder_Register_API.CreateRegistrationRequest(ssa);

            var api = new Infrastructure.API
            {
                URL = $"{DH_MTLS_GATEWAY_URL}/connect/register/{invalidClientId}",
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Put,             
                AccessToken = await new DataHolderAccessToken(clientId).GetAccessToken(),
                Content = new StringContent(registrationRequest, Encoding.UTF8, "application/jwt"),
                ContentType = MediaTypeHeaderValue.Parse("application/jwt"),
                Accept = "application/json"
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
                    Assert_HasHeader(@"Bearer error=""invalid_request"", error_description=""The client is unknown""",
                       response.Headers, "WWW-Authenticate");
                }
            }
        }
    }
}