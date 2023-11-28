#undef DEBUG_WRITE_EXPECTED_AND_ACTUAL_JSON

using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Extensions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Jose;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Xunit;
using Xunit.DependencyInjection;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests.JARM
{
    /*
     * MJS - CDRAuthServer tests are run in the CDRAuthServer pipeline, but also run again in the MDH/MDHE pipelines where they test the CdrAuthserver when
     * it's running embedded inside MDH/MDHE, ie, testing MDH/MDHE should just happen implicity when CDRAuthServer tests are MDH/MDHE pipeline.
     */

    // JARM - Authorise related tests
    public class US44264_CdrAuthServer_JARM_Authorise : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public US44264_CdrAuthServer_JARM_Authorise(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, ISqlQueryService sqlQueryService, IDataHolderParService dataHolderParService, IApiServiceDirector apiServiceDirector, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            if (testOutputHelperAccessor is null)
            {
                throw new ArgumentNullException(nameof(testOutputHelperAccessor));
            }

            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        private class ParResponse
        {
            public string? request_uri;
            public int? expires_in;
        }

        [Fact]
        public async Task AC01_MDH_JARM_AC01_HappyPath_JARM_Response_Mode_jwt()
        {
            const string STATE = "S8NJ7uqk5fY4EjNvP_G_FtyJu6pUsvH9jsYni9dMAJw";

            var responseType = ResponseType.Code;

            var parResponseMessage = await _dataHolderParService.SendRequest(
                scope: _options.SCOPE,
                responseMode: ResponseMode.Jwt,
                responseType: responseType,
                state: STATE
            );

            var parResponseMessageContent = await parResponseMessage.Content.ReadAsStringAsync();
            parResponseMessage.StatusCode.Should().Be(HttpStatusCode.Created, because: parResponseMessageContent);

            var parResponse = JsonConvert.DeserializeObject<ParResponse>(parResponseMessageContent);

            var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
                   .WithUserId(Constants.Users.UserIdKamillaSmith)
                   .WithSelectedAccountIds(Constants.Accounts.AccountIdsAllKamillaSmith)
                   .WithResponseType(responseType)
                   .WithRequestUri(parResponse.request_uri)
                   .BuildAsync();


            HttpResponseMessage response = await authService.AuthoriseForJarm();

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
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

                if (_authServerOptions.JARM_ENCRYPTION_ON)
                {
                    Console.WriteLine("JARM ENC IS ON");
                    var encryptedJwt = new JwtSecurityTokenHandler().ReadJwtToken(encodedJwt);
                    encryptedJwt.Header["alg"].Should().Be("RSA-OAEP", because: "JARM Encryption is turned on.");
                    encryptedJwt.Header["enc"].Should().Be("A128CBC-HS256", because: "JARM Encryption is turned on.");
                    // Decrypt the JARM JWT.
                    var privateKeyCertificate = new X509Certificate2(Constants.Certificates.JwtCertificateFilename, Constants.Certificates.JwtCertificatePassword, X509KeyStorageFlags.Exportable);
                    var privateKey = privateKeyCertificate.GetRSAPrivateKey();
                    JweToken token = JWE.Decrypt(queryValueResponse, privateKey);
                    encodedJwt = token.Plaintext;
                }

                // Check claims of decode jwt
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(encodedJwt);
                jwt.Should().NotBeNull();

                jwt.Claim("iss").Value.Should().Be(_options.DH_TLS_AUTHSERVER_BASE_URL);
                jwt.Claim("aud").Value.Should().Be(authService.ClientId);
                jwt.Claim("exp").Value.Should().NotBeNullOrEmpty();
                jwt.Claim("code").Value.Should().NotBeNullOrEmpty();
                jwt.Claim("state").Value.Should().Be(STATE);
            }
        }
    }
}