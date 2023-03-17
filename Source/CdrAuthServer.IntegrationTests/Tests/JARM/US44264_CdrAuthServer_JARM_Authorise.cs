#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CdrAuthServer.IntegrationTests.Fixtures;
using CdrAuthServer.IntegrationTests.Infrastructure.API2;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;
using System.Web;
using System.IdentityModel.Tokens.Jwt;
using CdrAuthServer.IntegrationTests.Extensions;
using System.Security.Cryptography.X509Certificates;
using Jose;

namespace CdrAuthServer.IntegrationTests.JARM
{
    /*
     * MJS - CDRAuthServer tests are run in the CDRAuthServer pipeline, but also run again in the MDH/MDHE pipelines where they test the CdrAuthserver when
     * it's running embedded inside MDH/MDHE, ie, testing MDH/MDHE should just happen implicity when CDRAuthServer tests are MDH/MDHE pipeline.
     */

    // JARM - Authorise related tests
    public class US44264_CdrAuthServer_JARM_Authorise : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private class ParResponse
        {
            public string? request_uri;
            public int? expires_in;
        }

        [Fact]
        public async Task AC01_MDH_JARM_AC01_HappyPath_JARM_Response_Mode_jwt()
        {
            const string STATE = "S8NJ7uqk5fY4EjNvP_G_FtyJu6pUsvH9jsYni9dMAJw";

            var parResponseMessage = await DataHolder_Par_API.SendRequest(
                responseMode: "jwt",
                responseType: US44264_CdrAuthServer_JARM_DCR.AUTHORIZATIONCODEFLOW_RESPONSETYPE,
                state: STATE
            );
            var parResponseMessageContent = await parResponseMessage.Content.ReadAsStringAsync();
            parResponseMessage.StatusCode.Should().Be(HttpStatusCode.Created, because: parResponseMessageContent);

            var parResponse = JsonConvert.DeserializeObject<ParResponse>(parResponseMessageContent);

            var clientId = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID);

            HttpResponseMessage response = await new DataHolder_Authorise_APIv2_Headless
            {
                UserId = BaseTest.USERID_KAMILLASMITH,
                OTP = BaseTest.AUTHORISE_OTP,
                SelectedAccountIds = ACCOUNTIDS_ALL_KAMILLA_SMITH,
                Scope = SCOPE,
                TokenLifetime = 3600,
                SharingDuration = SHARING_DURATION,
                RequestUri = parResponse.request_uri,
                CertificateFilename = BaseTest.CERTIFICATE_FILENAME,
                CertificatePassword = BaseTest.CERTIFICATE_PASSWORD,
                ClientId = clientId,
                RedirectURI = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                JwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
                JwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD,
                ResponseType = US44264_CdrAuthServer_JARM_DCR.AUTHORIZATIONCODEFLOW_RESPONSETYPE
            }.Authorise2(
                redirectUrl: BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
                allowRedirect: false  // don't allow auto redirect
            );

            // Assert
            using (new AssertionScope())
            {
                // Check redirect
                response.StatusCode.Should().Be(HttpStatusCode.Redirect);

                // Check query string
                response.Headers.Location?.Query.Should().NotBeNullOrEmpty();
                var queryValues = HttpUtility.ParseQueryString(response.Headers.Location?.Query ?? throw new NullReferenceException());

                // Check query has "response" param
                var queryValueResponse = queryValues["response"];
                var encodedJwt = queryValueResponse;
                queryValueResponse.Should().NotBeNullOrEmpty();

                // Check for encrypted JARM JWT.
                if (queryValueResponse.Split('.').Length > 3)
                {
                    // Decrypt the JARM JWT.
                    var privateKeyCertificate = new X509Certificate2(JWT_CERTIFICATE_FILENAME, JWT_CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
                    var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                    JweToken token = JWE.Decrypt(queryValueResponse, privateKey);
                    encodedJwt = token.Plaintext;
                }

                // Check claims of decode jwt
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(encodedJwt);
                jwt.Should().NotBeNull();

                jwt.Claim("iss").Value.Should().Be(DH_TLS_IDENTITYSERVER_BASE_URL);
                jwt.Claim("aud").Value.Should().Be(clientId);
                jwt.Claim("exp").Value.Should().NotBeNullOrEmpty(); 
                jwt.Claim("code").Value.Should().NotBeNullOrEmpty();
                jwt.Claim("state").Value.Should().Be(STATE);
            }
        }   
    }
}