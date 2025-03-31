using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US15221_US12969_US15584_CdrAuthServer_Registration_POST : BaseTest, IClassFixture<BaseFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly IRegisterSsaService _registerSSAService;
        private readonly IDataHolderRegisterService _dataHolderRegisterService;

        private readonly string _latestSSAVersion = "3";

        public US15221_US12969_US15584_CdrAuthServer_Registration_POST(IOptions<TestAutomationOptions> options, IRegisterSsaService registerSSAService, IDataHolderRegisterService dataHolderRegisterService, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _registerSSAService = registerSSAService ?? throw new ArgumentNullException(nameof(registerSSAService));
            _dataHolderRegisterService = dataHolderRegisterService ?? throw new ArgumentNullException(nameof(dataHolderRegisterService));
        }

        // Get payload from a JWT. Need to convert JArray claims into a string[] otherwise when we re-sign the token the JArray is not properly serialized.
        private static Dictionary<string, object> GetJWTPayload(JwtSecurityToken jwt)
        {
            var payload = new Dictionary<string, object>();

            foreach (var kvp in jwt.Payload)
            {
                // Need to process JArray as shown below because Microsoft.IdentityModel.Json.Linq.JArray is protected.
                if (kvp.Value.GetType().Name == "JArray")
                {
                    var list = new List<string>();

                    foreach (var item in kvp.Value as IEnumerable ?? throw new NullReferenceException())
                    {
                        list.Add(item.ToString() ?? throw new NullReferenceException());
                    }

                    payload.Add(kvp.Key, list.ToArray());
                }
                else
                {
                    payload.Add(kvp.Key, kvp.Value);
                }
            }

            return payload;
        }

        [Fact]
        public async Task AC01_Post_WithUnregistedSoftwareProduct_ShouldRespondWith_201Created_CreatedProfile()
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            // Act
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa);
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                response.StatusCode.Should().Be(
                    HttpStatusCode.Created,
                    $"NB: response.Content is {responseContent}");

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    // Check for the registration response properties.
                    var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    Assert.True(json["redirect_uris"] is JArray && (json["redirect_uris"] as JArray).Any());
                    Assert.True(json["grant_types"] is JArray && (json["grant_types"] as JArray).Any());
                    Assert.True(json["response_types"] is JArray && (json["response_types"] as JArray).Any());
                    Assert.Equal("web", json["application_type"]);
                    Assert.Equal("data-recipient-software-product", json["software_roles"]);
                    Assert.Equal("PS256", json["token_endpoint_auth_signing_alg"]);
                    Assert.Equal("private_key_jwt", json["token_endpoint_auth_method"]);
                    Assert.Equal("PS256", json["id_token_signed_response_alg"]);
                    Assert.Equal("PS256", json["request_object_signing_alg"]);
                    Assert.Equal("openid profile common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:regular_payments:read bank:payees:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.der:read energy:electricity.usage:read cdr:registration", json["scope"]);
                    Assert.Equal(ssa, json["software_statement"]);
                }
            }
        }

        [Fact]
        public async Task AC01_Post_WithUnregistedSoftwareProduct_MandatoryOnlyFields_ShouldRespondWith_201Created_CreatedProfile()
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion, industry: ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums.Industry.ALL);

            // Act
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(
                ssa,
                applicationType: string.Empty,
                requestObjectSigningAlg: string.Empty,
                redirectUris: null);
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                response.StatusCode.Should().Be(
                    HttpStatusCode.Created,
                    $"NB: response.Content is {responseContent}");

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    Assertions.AssertHasContentTypeApplicationJson(response.Content);

                    // Check for the registration response properties.
                    var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    Assert.True(json["redirect_uris"] is JArray && (json["redirect_uris"] as JArray).Any());
                    Assert.True(json["grant_types"] is JArray && (json["grant_types"] as JArray).Any());
                    Assert.True(json["response_types"] is JArray && (json["response_types"] as JArray).Any());
                    Assert.Equal("web", json["application_type"]);
                    Assert.Equal("data-recipient-software-product", json["software_roles"]);
                    Assert.Equal("PS256", json["token_endpoint_auth_signing_alg"]);
                    Assert.Equal("private_key_jwt", json["token_endpoint_auth_method"]);
                    Assert.Equal("PS256", json["id_token_signed_response_alg"]);
                    Assert.Equal("PS256", json["request_object_signing_alg"]);
                    Assert.Equal("openid profile common:customer.basic:read common:customer.detail:read bank:accounts.basic:read bank:accounts.detail:read bank:transactions:read bank:regular_payments:read bank:payees:read energy:accounts.basic:read energy:accounts.detail:read energy:accounts.concessions:read energy:accounts.paymentschedule:read energy:billing:read energy:electricity.servicepoints.basic:read energy:electricity.servicepoints.detail:read energy:electricity.der:read energy:electricity.usage:read cdr:registration", json["scope"]);
                    Assert.Equal(ssa, json["software_statement"]);
                }
            }
        }

        [Fact]
        public async Task AC02_Post_WithRegistedSoftwareProduct_ShouldRespondWith_400BadRequest_DuplicateErrorResponse()
        {
            async Task Arrange(string ssa)
            {
                Helpers.AuthServer.PurgeAuthServerForDataholder(_options);

                var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa);
                var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception("Unable to register software product");
                }
            }

            // Arrange
            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);
            await Arrange(ssa);

            var expectedError = new DuplicateRegistrationForSoftwareIdException();

            // Act - Try to register the same product again
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa);
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC03_Post_WithValidSSA_Success_Created()
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);

            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa);

            // Act
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        [Fact]
        public async Task AC03_Post_WithValidButUnapprovedSSA_400BadRequest_UnapprovedSSAErrorResponse()
        {
            // Fake a SSA by signing with certificate that is not the Register certificate
            string CreateFakeSSA(string ssa)
            {
                var decodedSSA = new JwtSecurityTokenHandler().ReadJwtToken(ssa);

                var payload = GetJWTPayload(decodedSSA);

                // Sign with a non-SSA certicate (ie just use a data recipient certificate)
                var fakeSSA = Helpers.Jwt.CreateJWT(Constants.Certificates.CertificateFilename, Constants.Certificates.CertificatePassword, payload);

                return fakeSSA;
            }

            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);

            var expectedError = new InvalidSoftwareStatementException();

            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);

            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(CreateFakeSSA(ssa));

            // Act
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC06_Post_WithInvalidSSAPayload_400BadRequest_InvalidSSAPayloadResponse()
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);

            const string RedirectUri = "foo";
            var expectedError = new InvalidRedirectUriException(RedirectUri);

            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa, redirectUris: [RedirectUri]);

            // Act
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC07_Post_WithInvalidMetadata_400BadRequest_InvalidMetadataResponse()
        {
            // Arrange
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options);

            var expectedError = new TokenEndpointAuthSigningAlgClaimInvalidException();

            var ssa = await _registerSSAService.GetSSA(Constants.Brands.BrandId, Constants.SoftwareProducts.SoftwareProductId, _latestSSAVersion);
            var registrationRequest = _dataHolderRegisterService.CreateRegistrationRequest(ssa, tokenEndpointAuthSigningAlg: "HS256"); // HS256 is invalid metadata (ie should be PS256)

            // Act
            var response = await _dataHolderRegisterService.RegisterSoftwareProduct(registrationRequest);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }
    }
}
