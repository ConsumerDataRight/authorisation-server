#undef FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS // FIXME - MJS - the test using this code needs to be in MDH integration tests since it uses MDH endpoint (ie $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts")

using CdrAuthServer.IntegrationTests.Extensions;
using CdrAuthServer.IntegrationTests.Fixtures;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CdrAuthServer.IntegrationTests
{
    public class US17652_CdrAuthServer_ArrangementRevocation : BaseTest, IClassFixture<TestFixture>
    {
        static private async Task Arrange()
        {
            TestSetup.DataHolder_PurgeIdentityServer();
            await TestSetup.DataHolder_RegisterSoftwareProduct();
        }

        static int CountPersistedGrant(string persistedGrantType, string? key = null)
        {
            using var connection = new SqlConnection(IDENTITYSERVER_CONNECTIONSTRING);
            connection.Open();

            SqlCommand selectCommand;
            if (key != null)
            {
                selectCommand = new SqlCommand($"select count(*) from grants where granttype=@type and [key]=@key", connection);
                selectCommand.Parameters.AddWithValue("@key", key);
            }
            else
            {
                selectCommand = new SqlCommand($"select count(*) from grants where granttype=@type", connection);
            }
            selectCommand.Parameters.AddWithValue("@type", persistedGrantType);

            return selectCommand.ExecuteScalarInt32();
        }

        [Fact]
        // When an arrangement exists, revoking the arrangement should remove the arrangement and revoke any associated tokens
        public async Task AC01_Post_WithArrangementId_ShouldRespondWith_204NoContent_ArrangementRevoked()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,             
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION) 
            }.Authorise();

            // Arrange - Get token and cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act - Revoke CDR arrangement
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);

                await Assert_HasNoContent2(response.Content);

                // Assert - Check persisted grants no longer exist
                CountPersistedGrant("refresh_token").Should().Be(0);
                CountPersistedGrant("cdr_arrangement_grant", cdrArrangementId).Should().Be(0);
            }
        }

        static async Task RevokeCdrArrangement(string cdrArrangementId)
        {
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: cdrArrangementId);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new Exception("Error revoking cdr arrangement");
            }
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // When an arrangement has been revoked, trying to use associated access token should result in error (Unauthorised)
        public async Task AC02_GetAccounts_WithRevokedAccessToken_ShouldRespondWith_401Unauthorised(bool revokeArrangement, HttpStatusCode expectedStatusCode)
        {
#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
            static async Task<HttpResponseMessage> GetAccounts(string? accessToken)
            {
                var api = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = $"{DH_MTLS_GATEWAY_URL}/cds-au/v1/banking/accounts",
                    XV = "1",
                    XFapiAuthDate = DateTime.Now.ToUniversalTime().ToString("r"),
                    AccessToken = accessToken
                };
                var response = await api.SendAsync();

                return response;
            }
#endif            

            // Arrange
            await Arrange();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,             
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION) 
            }.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null || tokenResponse.AccessToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Arrange - Revoke the arrangement
            if (revokeArrangement)
            {
                await RevokeCdrArrangement(tokenResponse.CdrArrangementId);
            }

#if FIXME_MJS_BELONGS_IN_MDH_INTEGRATION_TESTS
            // Act - Use access token to get accounts. The access token should have been revoked because the arrangement was revoked
            var response = await GetAccounts(tokenResponse.AccessToken);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    await Assert_HasNoContent2(response.Content);
                }
            }
#endif            
        }

        [Theory]
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.BadRequest)]
        // When an arrangement has been revoked, trying to use associated refresh token to get newly minted access token should result in error (401Unauthorised)
        public async Task AC03_GetAccessToken_WithRevokedRefreshToken_ShouldRespondWith_401Unauthorised(bool revokeArrangement, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION) 
            }.Authorise();

            // Arrange - Get token response using authCode
            var tokenResponse = await DataHolder_Token_API.GetResponse(authCode);
            if (tokenResponse == null || tokenResponse.RefreshToken == null || tokenResponse.CdrArrangementId == null) throw new Exception("Unexpected token response");

            // Arrange - Revoke the arrangement
            if (revokeArrangement)
            {
                await RevokeCdrArrangement(tokenResponse.CdrArrangementId);
            }

            // Act - Use refresh token to get a new access token. The refresh token should have been revoked because the arrangement was revoked            
            var response = await DataHolder_Token_API.SendRequest(
                grantType: "refresh_token",
                refreshToken: tokenResponse?.RefreshToken,
                scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.OK)
                {
                    // Assert - Check json
                    var expectedResponse = @"{""error"":""invalid_grant"",""error_description"":""ERR-TKN-002: refresh_token is invalid""}";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid arrangementid should result in error (422UnprocessableEntity)
        public async Task AC04_POST_WithInvalidArrangementID_ShouldRespondWith_422UnprocessableEntity()
        {
            // Arrange
            await Arrange();

            // Act
            const string CdrArrangementId = "foo";
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: CdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

                var expectedResponse = $@"{{
                    ""errors"": [{{
                        ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",
                        ""title"": ""Invalid Consent Arrangement"",
                        ""detail"": ""{CdrArrangementId}"",
                        ""meta"": {{}}
                    }}]
                }}";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with arrangementid that is not associated with the DataRecipient should result in error (422UnprocessableEntity)
        public async Task AC05_POST_WithNonAssociatedArrangementID_ShouldRespondWith_422UnprocessableEntity()
        {
            static async Task<JWKS_Endpoint> ArrangeAdditionalDataRecipient()
            {
                // Patch Register for additional data recipient
                TestSetup.Register_PatchRedirectUri(
                    ADDITIONAL_SOFTWAREPRODUCT_ID,
                    ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);
                TestSetup.Register_PatchJwksUri(
                    ADDITIONAL_SOFTWAREPRODUCT_ID,
                    ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS);

                // Stand-up JWKS endpoint for additional data recipient
                var jwks_endpoint = new JWKS_Endpoint(
                    ADDITIONAL_SOFTWAREPRODUCT_JWKS_URI_FOR_INTEGRATION_TESTS,
                    ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                    ADDITIONAL_JWKS_CERTIFICATE_PASSWORD);
                jwks_endpoint.Start();

                // Register software product for additional data recipient
                await TestSetup.DataHolder_RegisterSoftwareProduct(
                    ADDITIONAL_BRAND_ID,
                    ADDITIONAL_SOFTWAREPRODUCT_ID,
                    ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                    ADDITIONAL_JWKS_CERTIFICATE_PASSWORD);

                return jwks_endpoint;
            }

            // Arrange
            await Arrange();
            await using var additional_jwks_endpoint = await ArrangeAdditionalDataRecipient();

            // Arrange - Get authcode and thus create a CDR arrangement for ADDITIONAL_SOFTWAREPRODUCT_ID client
            var additionalClientId = GetClientId(ADDITIONAL_SOFTWAREPRODUCT_ID);
            var requestUri = await PAR_GetRequestUri(
                scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, 
                clientId: additionalClientId,
                jwtCertificateForClientAssertionFilename: ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                jwtCertificateForClientAssertionPassword: ADDITIONAL_JWKS_CERTIFICATE_PASSWORD,
                jwtCertificateForRequestObjectFilename: ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                jwtCertificateForRequestObjectPassword: ADDITIONAL_JWKS_CERTIFICATE_PASSWORD,
                redirectUri: ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                sharingDuration: SHARING_DURATION);

            (var additional_authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,            
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                ClientId = additionalClientId,
                RedirectURI = ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                JwtCertificateFilename = ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                JwtCertificatePassword = ADDITIONAL_JWKS_CERTIFICATE_PASSWORD,
                RequestUri = requestUri 
            }.Authorise(redirectUrl: ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS);

            // Arrange - Get the cdrArrangementId created by ADDITIONAL_SOFTWAREPRODUCT_ID client
            var additional_cdrArrangementId = (await DataHolder_Token_API.GetResponse(
                additional_authCode,
                clientId: additionalClientId,
                redirectUri: ADDITIONAL_SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                jwkCertificateFilename: ADDITIONAL_JWKS_CERTIFICATE_FILENAME,
                jwkCertificatePassword: ADDITIONAL_JWKS_CERTIFICATE_PASSWORD
            ))?.CdrArrangementId;

            // Act - Have SOFTWAREPRODUCT_ID client attempt to revoke CDR arrangement created by ADDITIONAL_SOFTWAREPRODUCT_ID client
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(cdrArrangementId: additional_cdrArrangementId);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

                // Assert - Check json
                var expectedResponse = $@"{{
                    ""errors"": [
                        {{
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",
                            ""title"": ""Invalid Consent Arrangement"",
                            ""detail"": ""{additional_cdrArrangementId}"",
                            ""meta"": {{}}
                        }}
                    ]
                }}";
                await Assert_HasContent_Json(expectedResponse, response.Content);

                // Assert - Check persisted grants still exist
                CountPersistedGrant("refresh_token").Should().Be(1);
                CountPersistedGrant("cdr_arrangement", additional_cdrArrangementId).Should().Be(1);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientid should result in error (401Unauthorised)
        public async Task AC07_POST_WithInvalidClientId_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientId: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check json
                var expectedResponse = @"{""error"":""invalid_client"",""error_description"":""ERR-GEN-004: Client not found""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientassertiontype should result in error (401Unauthorised)
        public async Task AC08a_POST_WithInvalidClientAssertionType_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientAssertionType: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check json
                var expectedResponse = @"{""error"":""invalid_client"",""error_description"":""ERR-CLIENT_ASSERTION-003: client_assertion_type must be urn:ietf:params:oauth:client-assertion-type:jwt-bearer""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Fact]
        // Calling revocation endpoint with invalid clientassertion should result in error (401Unauthorised)
        public async Task AC08b_POST_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest()
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS)
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                clientAssertion: "foo");

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // Assert - Check json
                var expectedResponse = @"{""error"":""invalid_client"",""error_description"":""ERR-CLIENT_ASSERTION-005: Cannot read client_assertion.  Invalid format.""}";
                await Assert_HasContent_Json(expectedResponse, response.Content);
            }
        }

        [Theory]
        [InlineData(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, HttpStatusCode.NoContent)]
        [InlineData(ADDITIONAL_CERTIFICATE_FILENAME, ADDITIONAL_CERTIFICATE_PASSWORD, HttpStatusCode.BadRequest)]  // ie different holder of key
        // Calling revocation endpoint with different holder of key should result in error
        public async Task AC09_POST_WithDifferentHolderOfKey_ShouldRespondWith_400BadRequest(string jwtCertificateFilename, string jwtCertificatePassword, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            await Arrange();

            // Arrange - Get authcode            
            (var authCode, _) = await new DataHolder_Authorise_APIv2
            {
                UserId = USERID_KAMILLASMITH,
                OTP = AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS,
                RequestUri = await PAR_GetRequestUri(scope: US12963_CdrAuthServer_Token.SCOPE_TOKEN_ACCOUNTS, sharingDuration: SHARING_DURATION) 
            }.Authorise();

            // Arrange - Get cdrArrangementId
            var cdrArrangementId = (await DataHolder_Token_API.GetResponse(authCode))?.CdrArrangementId;

            // Act
            var response = await DataHolder_CDRArrangementRevocation_API.SendRequest(
                cdrArrangementId: cdrArrangementId,
                jwtCertificateFilename: jwtCertificateFilename,
                jwtCertificatePassword: jwtCertificatePassword);

            // Assert
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatusCode);

                if (expectedStatusCode != HttpStatusCode.NoContent)
                {
                    // Assert - Check json
                    var expectedResponse = @"{""error"":""invalid_client"",""error_description"":""ERR-JWT-004: client_assertion - token validation error""}";
                    await Assert_HasContent_Json(expectedResponse, response.Content);
                }
            }
        }
    }
}
