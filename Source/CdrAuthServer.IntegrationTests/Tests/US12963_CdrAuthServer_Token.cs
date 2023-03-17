using CdrAuthServer.IntegrationTests.Extensions;
using CdrAuthServer.IntegrationTests.Fixtures;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using FluentAssertions.Execution;
using IdentityServer4.Stores.Serialization;
using Jose;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Dapper;

#nullable enable

namespace CdrAuthServer.IntegrationTests
{
    static class JsonHelper
    {
        public static string ToJson(this object value)
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
            jsonSerializerSettings.Converters.Add(new ClaimConverter());
            jsonSerializerSettings.Converters.Add(new ClaimsPrincipalConverter());

            return JsonConvert.SerializeObject(value, jsonSerializerSettings);
        }
    }

    public class US12963_CdrAuthServer_Token : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        public const string SCOPE_TOKEN_ACCOUNTS = "openid bank:accounts.basic:read";
        public const string SCOPE_TOKEN_CUSTOMER = "openid common:customer.basic:read";
        public const string SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS = "openid bank:accounts.basic:read bank:transactions:read";
        public const string SCOPE_EXCEED = "openid bank:accounts.basic:read bank:transactions:read additional:scope";

        static void AssertAccessToken(string? accessToken)
        {
            accessToken.Should().NotBeNullOrEmpty();
            if (accessToken != null)
            {
                var decodedAccessToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "iss");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "sub");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "aud");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "exp");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "auth_time");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "cnf");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "client_id");
                decodedAccessToken.Claims.Should().ContainSingle(c => c.Type == "software_id");
                decodedAccessToken.Claims.Should().Contain(c => c.Type == "scope");
            }
        }

        static void AssertIdToken(string? idToken)
        {
            idToken.Should().NotBeNullOrEmpty();
            if (idToken != null)
            {
                // Decrypt the id token.
                var privateKeyCertificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                JweToken token = JWE.Decrypt(idToken, privateKey);
                var decryptedIdToken = token.Plaintext;

                var decodedIdToken = new JwtSecurityTokenHandler().ReadJwtToken(decryptedIdToken);
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "iss");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "sub");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "aud");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "exp");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "auth_time");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "acr");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "family_name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "given_name");
                decodedIdToken.Claims.Should().ContainSingle(c => c.Type == "updated_at");
            }
        }

        #region TEST_SCENARIO_A_IDTOKEN_AND_ACCESSTOKEN        
        [Fact]
        public async Task AC01_Post_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken()
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,                
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION) 
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertAccessToken(tokenResponse?.AccessToken);
                }
                else
                {
                    var responseContent = await responseMessage.Content.ReadAsStringAsync();
                    responseContent.Should().NotBe(responseContent);
                }
            }
        }

        [Fact]
        public async Task AC02_Put_ShouldRespondWith_405MethodNotSupportedOr404NotFound()
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS) 
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, usePut: true);

            // Assert
            using (new AssertionScope())
            {
                // Depending on what is being tested (gateway or direct to auth server service) a different
                // status code will be returned.  This is also true in the Sandbox and CDR environments, as
                // the error handling is performed by the gateway device.
                responseMessage.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
            }
        }

        [Theory]
        [InlineData("authorization_code", HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_GrantType_ShouldRespondWith_400BadRequest_InvalidRequestErrorResponse(string grantType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS) 
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, grantType: grantType);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = grantType switch
                    {
                        null => "{\"error\":\"unsupported_grant_type\", \"error_description\": \"ERR-GEN-014: grant_type not provided\"}",
                        "foo" => "{\"error\":\"unsupported_grant_type\", \"error_description\": \"ERR-GEN-015: unsupported grant_type\"}",
                        _ => throw new NotSupportedException()
                    };

                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(SOFTWAREPRODUCT_ID, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(DataHolder_Token_API.OMIT, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_ClientId_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(string clientId, HttpStatusCode expectedStatusCode)
        {
            if (clientId == BaseTest.SOFTWAREPRODUCT_ID) 
            {
                clientId = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID);
            }

            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS)
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(
                authCode,
                clientId: clientId,
                omitIssuer: clientId == DataHolder_Token_API.OMIT);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = clientId switch
                    {
                        DataHolder_Token_API.OMIT => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-CLIENT_ASSERTION-009: Invalid client_assertion - Missing 'iss' claim\"}",
                        "foo" => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-GEN-004: Client not found\"}",
                        _ => throw new NotSupportedException()
                    };
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(CLIENTASSERTIONTYPE, HttpStatusCode.OK)]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_ClientAssertionType_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(string clientAssertionType, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS)
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = clientAssertionType switch
                    {
                        null => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-CLIENT_ASSERTION-002: client_assertion_type not provided\"}",
                        "foo" => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-CLIENT_ASSERTION-003: client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer\"}",
                        _ => throw new NotSupportedException()
                    };
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.BadRequest)]
        public async Task AC03_Post_WithInvalidRequest_ClientAssertion_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(bool useClientAssertion, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS) 
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, useClientAssertion: useClientAssertion);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_client\",\"error_description\":\"ERR-GEN-019: client_assertion not provided\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        public enum AC04_TestType { valid_jwt, omit_iss, omit_aud, omit_exp, omit_jti, exp_backdated }
        [Theory]
        [InlineData(AC04_TestType.valid_jwt, HttpStatusCode.OK)]
        [InlineData(AC04_TestType.omit_iss, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.omit_aud, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.omit_exp, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.omit_jti, HttpStatusCode.BadRequest)]
        [InlineData(AC04_TestType.exp_backdated, HttpStatusCode.BadRequest)]
        public async Task AC04_Post_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest_InvalidClientErrorResponse(AC04_TestType testType, HttpStatusCode expectedStatusCode)
        {
            static string GenerateClientAssertion(AC04_TestType testType)
            {
                string ISSUER = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID).ToLower();

                var now = DateTime.UtcNow;

                var additionalClaims = new List<Claim>
                {
                     new Claim("sub", ISSUER),
                     new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer)
                };

                if (testType != AC04_TestType.omit_iss)
                {
                    additionalClaims.Add(new Claim("iss", "foo"));
                }

                string? aud = null;
                if (testType != AC04_TestType.omit_aud)
                {
                    aud = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/token";
                }

                if (testType != AC04_TestType.omit_jti)
                {
                    additionalClaims.Add(new Claim("jti", Guid.NewGuid().ToString()));
                }

                DateTime? expires = null;
                if (testType == AC04_TestType.exp_backdated)
                {
                    expires = now.AddMinutes(-1);
                }
                else if (testType != AC04_TestType.omit_exp)
                {
                    expires = now.AddMinutes(10);
                }

                var certificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                var x509SigningCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSsaPssSha256);

                var jwt = new JwtSecurityToken(
                    (testType == AC04_TestType.omit_iss) ? null : ISSUER,
                    aud,
                    additionalClaims,
                    expires: expires,
                    signingCredentials: x509SigningCredentials);

                var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

                return jwtSecurityTokenHandler.WriteToken(jwt);
            }

            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,             
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS) 
            }.Authorise();

            // Act
            var clientAssertion = GenerateClientAssertion(testType);
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, customClientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert error response
                    var expectedResponse = testType switch
                    {
                        AC04_TestType.omit_iss => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-CLIENT_ASSERTION-009: Invalid client_assertion - Missing 'iss' claim\"}",
                        AC04_TestType.omit_aud => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-JWT-001: client_assertion - Invalid audience\"}",
                        AC04_TestType.omit_exp => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-JWT-004: client_assertion - token validation error\"}",
                        AC04_TestType.omit_jti => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-GEN-017: Invalid client_assertion - 'jti' is required\"}",
                        AC04_TestType.exp_backdated => "{\"error\":\"invalid_client\", \"error_description\": \"ERR-JWT-002: client_assertion has expired\"}",  // FIXME - MJS - what message?
                        _ => throw new NotSupportedException()
                    };
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        public async Task AC05_Post_WithExpiredAuthCode_ShouldRespondWith_400BadRequest_InvalidGrant(bool expired, HttpStatusCode expectedStatusCode)
        {
          
            static async Task ExpireAuthCode(string authCode)
            {
                using var connection = new SqlConnection(IDENTITYSERVER_CONNECTIONSTRING);
                connection.Open();

                var count = await connection.ExecuteAsync("update grants set expiresat = @expiresAt where [key]=@key", new
                {
                    expiresAt = DateTime.UtcNow.AddDays(-90),
                    key = authCode
                });

                if (count != 1)
                {
                    throw new Exception($"No grant found for authcode '{authCode}'");
                }
            }

            // Arrange
            TestSetup.DataHolder_PurgeIdentityServer(true);

            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS) 
            }.Authorise();

            if (expired)
            {
                await ExpireAuthCode(authCode);
            }

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_grant\",\"error_description\":\"ERR-TKN-005: authorization code has expired\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC06_Post_WithMissingAuthCode_ShouldRespondWith_400BadRequest_InvalidGrant(string authCode, HttpStatusCode expectedStatusCode)
        {
            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    var expectedResponse = "{\"error\":\"invalid_request\",\"error_description\":\"ERR-GEN-035: code is missing\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        public async Task AC07_Post_WithInvalidAuthCode_ShouldRespondWith_400BadRequest_InvalidGrantResponse(string authCode, HttpStatusCode expectedStatusCode)
        {
            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode: authCode);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    var expectedResponse = "{\"error\":\"invalid_grant\",\"error_description\":\"ERR-TKN-007: authorization code is invalid\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }
        #endregion

        #region TEST_SCENARIO_B_IDTOKEN_ACCESSTOKEN_REFRESHTOKEN
        [Theory]
        [InlineData(3600)]
        public async Task AC08_Post_WithShareDuration_ShouldRespondWith_200OK_IDToken_AccessToken_RefreshToken(int shareDuration)
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                SharingDuration = 100000,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION)
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, shareDuration: shareDuration);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Assert_HasContentType_ApplicationJson(responseMessage.Content);

                    var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    AssertIdToken(tokenResponse?.IdToken);
                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }
        #endregion

        #region TEST_SCENARIO_C_USE_REFRESHTOKEN_FOR_NEW_ACCESSTOKEN
        private async Task<HttpResponseMessage> Test_AC09_AC10(string initalScope, string requestedScope)
        {
            static async Task<(string? authCode, string? refreshToken)> GetRefreshToken(string scope)
            {
                // Create grant with specific scope
                (var authCode, _) = await new DataHolder_Authorise_APIv2
                {
                    UserId = USERID_KAMILLASMITH,
                    OTP = AUTHORISE_OTP,
                    SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                    Scope = scope,
                    SharingDuration = 100000,
                    RequestUri = await PAR_GetRequestUri(scope: scope, sharingDuration: 100000)
                }.Authorise();

                var responseMessage = await DataHolder_Token_API.SendRequest(authCode, shareDuration: 3600);

                var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                if (tokenResponse?.RefreshToken == null)
                {
                    throw new Exception($"{nameof(AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken)}.{nameof(GetRefreshToken)} - Error getting refresh token");
                }

                // Just make sure refresh token was issued with correct scope
                if (tokenResponse?.Scope != scope)
                {
                    throw new Exception($"{nameof(AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken)}.{nameof(GetRefreshToken)} - Unexpected scope");
                }

                return (authCode, tokenResponse?.RefreshToken);
            }

            // Get a refresh token with initial scope
            var (authCode, refreshToken) = await GetRefreshToken(initalScope);

            // Use the refresh token to get a new accesstoken and new refreshtoken (with the requested scope)
            var responseMessage = await DataHolder_Token_API.SendRequest(
                grantType: "refresh_token",
                refreshToken: refreshToken,
                scope: requestedScope);

            return responseMessage;
        }

        [Fact]
        public async Task AC09_Post_WithRefreshToken_AndSameScope_ShouldRespondWith_200OK_AccessToken_RefreshToken()
        {
            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var tokenResponse = await DataHolder_Token_API.DeserializeResponse(responseMessage);
                    tokenResponse.Should().NotBeNull();
                    tokenResponse?.TokenType.Should().Be("Bearer");
                    tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS);
                    tokenResponse?.CdrArrangementId.Should().NotBeNullOrEmpty();
                    tokenResponse?.Scope.Should().Be(SCOPE_TOKEN_ACCOUNTS);
                    tokenResponse?.AccessToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();
                    tokenResponse?.IdToken.Should().NotBeNullOrEmpty();

                    AssertIdToken(tokenResponse?.IdToken);
                    AssertAccessToken(tokenResponse?.AccessToken);
                }
            }
        }

        [Theory]
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS, HttpStatusCode.OK)] // Same scope - should be ok
        [InlineData(SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS, SCOPE_TOKEN_ACCOUNTS, HttpStatusCode.OK)] // Decreased in scope - should be ok
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_TOKEN_ACCOUNTS_AND_TRANSACTIONS, HttpStatusCode.BadRequest)] 
        [InlineData(SCOPE_TOKEN_ACCOUNTS, SCOPE_EXCEED, HttpStatusCode.BadRequest)] 
        public async Task AC10_Post_WithRefreshToken_AndDifferentScope_ShouldRespondWith_ExpectedResponse(string initialScope, string requestedScope, HttpStatusCode expectedStatusCode)
        {
            // Arrange/Act
            var responseMessage = await Test_AC09_AC10(initialScope, requestedScope);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert error response
                    var expectedResponse = "{\"error\":\"invalid_scope\",\"error_description\":\"Additional scopes were requested in the refresh_token request\"}";
                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }

        [Theory]
        [InlineData("foo", HttpStatusCode.BadRequest)]
        [InlineData(null, HttpStatusCode.BadRequest)]
        public async Task AC11_Post_WithInvalidRefreshToken_ShouldRespondWith_400BadRequest_InvalidFieldErrorResponse(string refreshToken, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: SCOPE_TOKEN_ACCOUNTS)
            }.Authorise();

            // Act
            var responseMessage = await DataHolder_Token_API.SendRequest(authCode, grantType: "refresh_token", refreshToken: refreshToken);

            // Assert
            using (new AssertionScope())
            {
                responseMessage.StatusCode.Should().Be(expectedStatusCode);

                if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Assert error response
                    var expectedResponse = refreshToken switch
                    {
                        null => "{\"error\":\"invalid_grant\", \"error_description\": \"ERR-TKN-003: refresh_token is missing\"}",
                        "foo" => "{\"error\":\"invalid_grant\", \"error_description\": \"ERR-TKN-002: refresh_token is invalid\"}",
                        _ => throw new NotSupportedException()
                    };

                    await Assert_HasContent_Json(expectedResponse, responseMessage.Content);
                }
            }
        }
        #endregion

        #region EXTRA_TESTS
        [Theory]
        [InlineData(null, false)]
        [InlineData(0, false)]
        [InlineData(10000, true)]
        public async Task ACX01_Authorise_WithSharingDuration_ShouldRespondWith_AccessTokenRefreshToken(int? sharingDuration, bool expectsRefreshToken)
        {
            var nowEpoch = DateTime.UtcNow.UnixEpoch();

            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                SharingDuration = sharingDuration,
                RequestUri = await PAR_GetRequestUri(sharingDuration: sharingDuration) 
            }.Authorise();

            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            // Assert
            using (new AssertionScope())
            {
                tokenResponse.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS); // access token expiry
                tokenResponse?.CdrArrangementId.Should().NotBeNull();
                tokenResponse?.CdrArrangementId.Should().NotBeEmpty();

                if (expectsRefreshToken)
                {
                    tokenResponse?.RefreshToken.Should().NotBeNullOrEmpty();

                    var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse?.AccessToken);
                  
                }
                else
                    tokenResponse?.RefreshToken.Should().BeNullOrEmpty();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task ACX02_UseRefreshTokenMultipleTimes_ShouldRespondWith_AccessTokenRefreshToken(int usageAttempts)
        {
            // Arrange - Get authcode, 
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,             
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                SharingDuration = 10000,
                RequestUri = await PAR_GetRequestUri(sharingDuration: SHARING_DURATION) 
            }.Authorise();

            // Act - Get access token and refresh token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            using (new AssertionScope())
            {
                // Assert 
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNull();
                tokenResponse?.RefreshToken.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS); // access token expiry

                var refreshToken = tokenResponse?.RefreshToken;
                for (int i = 1; i <= usageAttempts; i++)
                {
                    // Act - Use refresh token to get access token and refresh token
                    var refreshTokenResponse = await DataHolder_Token_API.GetResponseUsingRefreshToken(refreshToken);

                    // Assert
                    refreshTokenResponse.Should().NotBeNull();
                    refreshTokenResponse?.AccessToken.Should().NotBeNull();
                    refreshTokenResponse?.RefreshToken.Should().NotBeNull();
                    refreshTokenResponse?.RefreshToken.Should().Be(refreshToken); // same refresh token is returned 

                    refreshToken = refreshTokenResponse?.RefreshToken;
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ACX03_UseExpiredRefreshToken_ShouldRespondWith_400BadRequest(bool expired)
        {
            const int SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN = 10;

            var nowEpoch = DateTime.UtcNow.UnixEpoch();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,                
                RequestUri = await PAR_GetRequestUri(sharingDuration: SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN) 
            }.Authorise();

            // Act - Get access token and refresh token
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);

            using (new AssertionScope())
            {
                // Assert 
                tokenResponse.Should().NotBeNull();
                tokenResponse?.AccessToken.Should().NotBeNull();
                tokenResponse?.RefreshToken.Should().NotBeNull();
                tokenResponse?.ExpiresIn.Should().Be(TOKEN_EXPIRY_SECONDS); // access token expiry

                if (expired)
                {
                    // Assert - Check that refresh token will expire when we expect it to
                    var decodedJWT = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse?.AccessToken);

                    // Arrange - wait until refresh token has expired
                    await Task.Delay((SHARING_DURATION_FOR_EXPIRED_REFRESHTOKEN + 10) * 1000);
                }

                // Act - Use refresh token to get access token and refresh token
                var refreshTokenResponseMessage = await DataHolder_Token_API.SendRequest(grantType: "refresh_token", refreshToken: tokenResponse?.RefreshToken);

                // Assert - If expired should be BadRequest otherwise OK
                refreshTokenResponseMessage.StatusCode.Should().Be(expired ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
            }
        }

        static private async Task<HttpStatusCode> CallResourceAPI(string accessToken)
        {
            var api = new Infrastructure.API
            {
                CertificateFilename = CERTIFICATE_FILENAME,
                CertificatePassword = CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Get,
                URL = $"{DH_MTLS_GATEWAY_URL}/resource/cds-au/v1/common/customer",
                XV = "1",
                XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                AccessToken = accessToken
            };
            var response = await api.SendAsync();

            return response.StatusCode;
        }
    }
    #endregion
}