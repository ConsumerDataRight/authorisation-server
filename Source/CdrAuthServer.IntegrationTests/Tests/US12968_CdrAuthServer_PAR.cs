using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using CdrAuthServer.IntegrationTests.Fixtures;

#nullable enable

namespace CdrAuthServer.IntegrationTests
{
    public class US12968_CdrAuthServer_PAR : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        [Fact]
        // Call PAR endpoint, with request, to get a RequestUri
        public async Task AC01_Post_ShouldRespondWith_201Created_RequestUri()
        {
            // Arrange

            // Act
            var response = await DataHolder_Par_API.SendRequest();

            var responseText = await response.Content.ReadAsStringAsync();

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
                    parResponse.Should().NotBeNull();
                    parResponse?.RequestURI.Should().NotBeNullOrEmpty();
                    parResponse?.ExpiresIn.Should().Be(90);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)] // Unknown ArrangementID
        public async Task AC06_Post_WithUnknownOrUnAssociatedCdrArrangementId_ShouldRespondWith_400BadRequest(string cdrArrangementId, HttpStatusCode expectedStatusCode)
        {
            // Arrange

            // Act
            var response = await DataHolder_Par_API.SendRequest(cdrArrangementId: cdrArrangementId);            

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check application/json
                    Assert_HasContentType_ApplicationJson(response.Content);

                    var expectedResponse = @"{""error"":""invalid_request_object"",""error_description"":""ERR-GEN-029: Invalid cdr_arrangement_id""}";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.Created)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC08_Post_WithInvalidClientId_ShouldRespondWith_400BadRequest(string clientId, HttpStatusCode expectedStatusCode)
        {
            if (clientId == BaseTest.SOFTWAREPRODUCT_ID) 
            {           
                clientId = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID);
            }

            // Arrange

            // Act
            var response = await DataHolder_Par_API.SendRequest(clientId: clientId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_client"",
                        ""error_description"": ""ERR-GEN-004: Client not found""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(CLIENTASSERTIONTYPE, HttpStatusCode.Created)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC09_Post_WithInvalidClientAssertionType_ShouldRespondWith_400BadRequest(string clientAssertionType, HttpStatusCode expectedStatusCode)
        {
            // Arrange

            // Act
            var response = await DataHolder_Par_API.SendRequest(clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_client"",
                        ""error_description"": ""ERR-CLIENT_ASSERTION-003: client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC10_Revocation_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest(string clientAssertion, HttpStatusCode expectedStatusCode)
        {
            // Arrange

            // Act
            var response = await DataHolder_Par_API.SendRequest(clientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_client"",
                        ""error_description"": ""ERR-CLIENT_ASSERTION-005: Cannot read client_assertion.  Invalid format.""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Theory]
        [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.Created)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.BadRequest)] // ie different holder of key
        public async Task AC11_Post_WithDifferentHolderOfKey_ShouldRespondWith_400BadRequest(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Act
            var response = await DataHolder_Par_API.SendRequest(
                jwtCertificateForClientAssertionFilename: jwtCertificateFilename,
                jwtCertificateForClientAssertionPassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.Created)
                {
                    var expectedResponse = @"{
                        ""error"": ""invalid_client"",
                        ""error_description"": ""ERR-JWT-004: client_assertion - token validation error""
                    }";

                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, to create CdrArrangement
        public async Task AC02_Post_AuthorisationEndpoint_WithRequestUri_ShouldRespondWith_200OK_CdrArrangementId()
        {
            // Arrange
            var response = await DataHolder_Par_API.SendRequest();
            if (response.StatusCode != HttpStatusCode.Created) throw new Exception("Error with PAR request - StatusCode");
            var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("Error with PAR request - RequestURI");
            if (parResponse?.ExpiresIn != 90) throw new Exception("Error with PAR request - ExpiresIn");

            // Act - Authorise with PAR RequestURI
            (var authCode, var idToken) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = parResponse.RequestURI
            }.Authorise();

            // Assert - Check we got an authCode and idToken
            using (new AssertionScope())
            {
                authCode.Should().NotBeNullOrEmpty();
                idToken.Should().NotBeNullOrEmpty();
            }

            // Act - Use the authCode to get token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            // Assert - Check we get back cdrArrangementId
            using (new AssertionScope())
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
            }
        }

        [Theory]
        [InlineData(
            HttpStatusCode.Redirect,
            SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
            "error=invalid_request_uri&error_description=ERR-AUTH-006"
        )]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, but after requestUri has expired (90 seconds), should redirect to DH callback URI
        public async Task AC03_Post_AuthorisationEndpoint_WithRequestUri_After90Seconds_ShouldRespondWith_302Found_CallbackURI(HttpStatusCode expectedStatusCode, string expectedRedirectPath, string? expectedRedirectFragment = null)
        {
            expectedRedirectPath = BaseTest.SubstituteConstant(expectedRedirectPath);

            static HttpClient CreateHttpClient()
            {
                var httpClientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };
                httpClientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                httpClientHandler.ClientCertificates.Add(new X509Certificate2(CERTIFICATE_FILENAME, CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable));
                var httpClient = new HttpClient(httpClientHandler);
                return httpClient;
            }

            const int PAR_EXPIRY_SECONDS = 90;

            // Arrange
            var response = await DataHolder_Par_API.SendRequest();
            if (response.StatusCode != HttpStatusCode.Created) throw new Exception("Error with PAR request - StatusCode");
            var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
            if (string.IsNullOrEmpty(parResponse?.RequestURI)) throw new Exception("Error with PAR request - RequestURI");
            if (parResponse?.ExpiresIn != PAR_EXPIRY_SECONDS) throw new Exception("Error with PAR request - ExpiresIn");

            // Wait until PAR expires
            await Task.Delay((PAR_EXPIRY_SECONDS + 10) * 1000);

            var AuthorisationURL = new AuthoriseURLBuilder { RequestUri = parResponse.RequestURI }.URL;
            var request = new HttpRequestMessage(HttpMethod.Get, AuthorisationURL);
            BaseTest.AttachHeadersForStandAlone(request.RequestUri?.AbsoluteUri ?? throw new NullReferenceException(), request.Headers);
            var authResponse = await CreateHttpClient().SendAsync(request);

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                authResponse.StatusCode.Should().Be(expectedStatusCode);

                // Check redirect path
                var redirectPath = authResponse?.Headers?.Location?.GetLeftPart(UriPartial.Path);
                redirectPath.Should().Be(expectedRedirectPath);

                // Check redirect query
                if (expectedRedirectFragment != null)
                {
                    var redirectFragment = HttpUtility.UrlDecode(authResponse?.Headers?.Location?.Fragment.TrimStart('#'));
                    redirectFragment.Should().StartWith(HttpUtility.UrlDecode(expectedRedirectFragment));
                }
            }
        }

        [Fact]
        // Call PAR endpoint, with existing CdrArrangementId, to get requestUri
        public async Task<(string? cdrArrangementId, string? requestUri)> AC04_Post_WithCdrArrangementId_ShouldRespondWith_201Created_RequestUri()
        {
            // Create a CDR arrangement
            static async Task<string> CreateCDRArrangement()
            {
                // Authorise
                (var authCode, var _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = USERID_KAMILLASMITH,
                    OTP = AUTHORISE_OTP,
                    SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                    Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                    RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION)
                }.Authorise();

                // Get token
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode, scope:US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
                if (tokenResponse == null || tokenResponse?.CdrArrangementId == null) throw new Exception("Error getting CDRArrangementId");

                // Return CdrArrangementId
                return tokenResponse.CdrArrangementId;
            }

            // Arrange 
            var cdrArrangementId = await CreateCDRArrangement();

            // Act - PAR with existing CdrArrangementId
            var response = await DataHolder_Par_API.SendRequest(cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                var parResponse = await DataHolder_Par_API.DeserializeResponse(response);
                parResponse.Should().NotBeNull();
                parResponse?.RequestURI.Should().NotBeNullOrEmpty();
                parResponse?.ExpiresIn.Should().Be(90);

                return (cdrArrangementId, parResponse?.RequestURI);
            }
        }

        [Fact]
        // Call Authorisaton endpoint, with requestUri issued by PAR endpoint, to update existing CdrArrangement
        public async Task AC05_Post_AuthorisationEndpoint_WithRequestUri_ShouldRespondWith_200OK_CdrArrangementId()
        {
            // Arrange
            // Create CDR arrangement, call PAR and get RequestURI
            (var cdrArrangementId, var requestUri) = await AC04_Post_WithCdrArrangementId_ShouldRespondWith_201Created_RequestUri();
            if (string.IsNullOrEmpty(requestUri)) throw new Exception("requestUri is null");

            // Act - Authorise using requestURI
            (var authCode, var _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = requestUri
            }.Authorise();

            // Act - Use the authCode to get token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);

            // Assert - Check we get back cdrArrangementId
            using (new AssertionScope())
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty(); 
                tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                tokenResponse?.Scope.Should().Be(US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
            }
        }

        [Fact]
        // Call PAR endpoint, with existing CdrArrangementId, to get requestUri
        public async Task AC04_Post_WithCdrArrangementId_AmendingConsent_ShouldInvalidateRefreshTokenn()
        {
            // Create a CDR arrangement
            static async Task<(string cdrArrangementId, string refreshToken)> CreateCDRArrangement(string cdrArrangementId = null)
            {
                // Authorise
                (var authCode, var _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = USERID_KAMILLASMITH,
                    OTP = AUTHORISE_OTP,                 
                    SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                    Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                    RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION, cdrArrangementId: cdrArrangementId) // MJS - BREAKINGCHANGE-FAPI-PHASE2
                }.Authorise();

                // Get token
                var tokenResponse = await DataHolder_Token_API.GetResponse(authCode, scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);
                if (tokenResponse == null || tokenResponse?.CdrArrangementId == null) throw new Exception("Error getting CDRArrangementId");

                // Return CdrArrangementId
                return (tokenResponse.CdrArrangementId, tokenResponse.RefreshToken);
            }

            // Arrange 
            var (cdrArrangementId, refreshToken) = await CreateCDRArrangement();

            // Amend consent.
            var (newCrArrangementId, newRefreshToken) = await CreateCDRArrangement(cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                Assert.Equal(cdrArrangementId, newCrArrangementId);
                Assert.NotEqual(refreshToken, newRefreshToken);
            }
        }
    }
}
