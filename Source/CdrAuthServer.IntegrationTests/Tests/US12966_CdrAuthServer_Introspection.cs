using CdrAuthServer.IntegrationTests.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Exceptions.AuthoriseExceptions;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Fixtures;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using Xunit;
using Xunit.DependencyInjection;
using Constants = ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Constants;

namespace CdrAuthServer.IntegrationTests
{
    public class US12966_CdrAuthServer_Introspection : BaseTest, IClassFixture<RegisterSoftwareProductFixture>
    {
        private readonly TestAutomationOptions _options;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderIntrospectionService _dataHolderIntrospectionService;

        public US12966_CdrAuthServer_Introspection(IOptions<TestAutomationOptions> options, IAuthorizationService authorizationService, ISqlQueryService sqlQueryService, IDataHolderIntrospectionService dataHolderIntrospectionService, ITestOutputHelperAccessor testOutputHelperAccessor, IConfiguration config)
            : base(testOutputHelperAccessor, config)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderIntrospectionService = dataHolderIntrospectionService ?? throw new ArgumentNullException(nameof(dataHolderIntrospectionService));
        }

        private void Arrange()
        {
            Helpers.AuthServer.PurgeAuthServerForDataholder(_options, true);
        }

        private class IntrospectionResponse
        {
#pragma warning disable IDE1006
            public bool? active { get; set; }

            public string? scope { get; set; }

            public int? exp { get; set; }

            public string? cdr_arrangement_id { get; set; }
#pragma warning restore IDE1006
        }

        // Sort space delimited array, eg Sort("dog cat frog") returns "cat dog frog"
        private static string? Sort(string? s)
        {
            if (s == null)
            {
                return null;
            }

            var array = s.Split(' ');

            Array.Sort(array);

            return string.Join(' ', array);
        }

        [Theory]
        [InlineData(TokenType.KamillaSmith)]
        public async Task AC01_Post_ShouldRespondWith_200OK_IntrospectionInfo(TokenType tokenType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(tokenType), tokenType);

            const int REFRESHTOKEN_LIFETIME_SECONDS = Constants.AuthServer.Days90;
            const int EXPIRY_GRACE_SECONDS = 120; // Grace period for expiry check (ie window size)

            // Arrange
            Arrange();
            var approximateGrantTime_Epoch = (int)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
            var tokenResponse = await _authorizationService.GetToken(tokenType);

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(token: tokenResponse.RefreshToken);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(true);
                Sort(actual?.scope).Should().Be(Sort(tokenResponse.Scope));
                actual?.cdr_arrangement_id.Should().Be(tokenResponse.CdrArrangementId);

                // Check expiry of refresh token.
                // Since we only know approximate time that refresh token was granted, we can only know approximate time refresh token will expire
                var approximateExpiryTime_Epoch = approximateGrantTime_Epoch + REFRESHTOKEN_LIFETIME_SECONDS;

                // So expiry time is approximated, check that actual expiry is within small window of the approximate expiry time
                actual?.exp.Should().BeInRange(
                    approximateExpiryTime_Epoch - EXPIRY_GRACE_SECONDS,
                    approximateExpiryTime_Epoch + EXPIRY_GRACE_SECONDS);
            }
        }

        [Theory]
        [InlineData(false, true)] // valid refresh token, expect active to be true
        [InlineData(true, false)] // invalid refresh token, expect active to be false
        public async Task AC02_Post_WithInvalidRefreshToken_ShouldRespondWith_200OK_ActiveFalse(bool invalidRefreshToken, bool expectedActive)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}", nameof(invalidRefreshToken), invalidRefreshToken, nameof(expectedActive), expectedActive);

            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: invalidRefreshToken ? "foo" : tokenResponse.RefreshToken);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(expectedActive);
            }
        }

        [Theory]
        [InlineData(false, true)] // valid refresh token, expect active to be true
        [InlineData(true, false)] // missing refresh token, expect active to be false
        public async Task AC03_Post_WithMissingRefreshToken_ShouldRespondWith_200OK_ActiveFalse(bool missingRefreshToken, bool expectedActive)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(missingRefreshToken), missingRefreshToken, nameof(expectedActive), expectedActive);

            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: missingRefreshToken ? null : tokenResponse.RefreshToken,
                tokenTypeHint: missingRefreshToken ? null : "refresh_token");

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(expectedActive);
            }
        }

        [Theory]
        [InlineData(false, true)] // valid refresh token, expect active to be true
        [InlineData(true, false)] // expired refresh token, expect active to be false
        public async Task AC04_Post_WithExpiredRefreshToken_ShouldRespondWith_200OK_ActiveFalse(bool expired, bool expectedActive)
        {
            Log.Information("Running test with Params: {P1}={V1}, {P2}={V2}.", nameof(expired), expired, nameof(expectedActive), expectedActive);

            // Arrange
            Arrange();

            ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Dataholders.TokenResponse tokenResponse;
            if (expired)
            {
                const int EXPIRED_LIFETIME_SECONDS = 10;

                tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith, tokenLifetime: EXPIRED_LIFETIME_SECONDS, sharingDuration: EXPIRED_LIFETIME_SECONDS);

                // Wait for token to expire
                await Task.Delay((EXPIRED_LIFETIME_SECONDS + 5) * 1000);
            }
            else
            {
                tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);
            }

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(token: tokenResponse.RefreshToken);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<IntrospectionResponse>(actualJson);

                actual.Should().NotBeNull();
                actual?.active.Should().Be(expectedActive);
            }
        }

        [Fact]
        public async Task AC06_Post_WithValidClientId_Success()
        {
            var clientId = _options.LastRegisteredClientId;

            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);
            _ = new ClientNotFoundException();

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: tokenResponse.RefreshToken,
                clientId: clientId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC06_Post_WithInvalidClientId_ShouldRespondWith_400BadRequest_ErrorResponse(string clientId)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientId), clientId);

            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            var expectedError = new ClientNotFoundException();

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: tokenResponse.RefreshToken,
                clientId: clientId);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC07_Post_WithValidClientAssertionType_Success()
        {
            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: tokenResponse.RefreshToken,
                clientAssertionType: Constants.ClientAssertionType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC07_Post_WithInvalidClientAssertionType_ShouldRespondWith_400BadRequest_ErrorResponse(string clientAssertionType)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientAssertionType), clientAssertionType);

            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            var expectedError = new InvalidClientAssertionTypeException();

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: tokenResponse.RefreshToken,
                clientAssertionType: clientAssertionType);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }

        [Fact]
        public async Task AC08_Post_WithNullClientAssertion_Success()
        {
            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: tokenResponse.RefreshToken,
                clientAssertion: null);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Theory]
        [InlineData("foo")]
        public async Task AC08_Post_WithInvalidClientAssertion_ShouldRespondWith_400BadRequest_ErrorResponse(string clientAssertion)
        {
            Log.Information("Running test with Params: {P1}={V1}.", nameof(clientAssertion), clientAssertion);

            // Arrange
            Arrange();
            var tokenResponse = await _authorizationService.GetToken(TokenType.KamillaSmith);

            var expectedError = new InvalidClientAssertionFormatException();

            // Act
            var response = await _dataHolderIntrospectionService.SendRequest(
                token: tokenResponse.RefreshToken,
                clientAssertion: clientAssertion);

            // Assert
            using (new AssertionScope(BaseTestAssertionStrategy))
            {
                await Assertions.AssertErrorAsync(response, expectedError);
            }
        }
    }
}
