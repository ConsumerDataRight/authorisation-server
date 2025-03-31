using CdrAuthServer.IntegrationTests.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Enums;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Interfaces;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Dataholders;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Models.Options;
using ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation.Services;
using Microsoft.Extensions.Options;

namespace CdrAuthServer.IntegrationTests.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly TestAutomationOptions _options;
        private readonly TestAutomationAuthServerOptions _authServerOptions;

        private readonly IDataHolderTokenService _dataHolderTokenService;
        private readonly ISqlQueryService _sqlQueryService;
        private readonly IDataHolderParService _dataHolderParService;
        private readonly IApiServiceDirector _apiServiceDirector;

        public AuthorizationService(IOptions<TestAutomationOptions> options, IOptions<TestAutomationAuthServerOptions> authServerOptions, IDataHolderTokenService dataHolderTokenService, ISqlQueryService sqlQueryService, IDataHolderParService dataHolderParService, IApiServiceDirector apiServiceDirector)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _authServerOptions = authServerOptions.Value ?? throw new ArgumentNullException(nameof(authServerOptions));
            _dataHolderTokenService = dataHolderTokenService ?? throw new ArgumentNullException(nameof(dataHolderTokenService));
            _sqlQueryService = sqlQueryService ?? throw new ArgumentNullException(nameof(sqlQueryService));
            _dataHolderParService = dataHolderParService ?? throw new ArgumentNullException(nameof(dataHolderParService));
            _apiServiceDirector = apiServiceDirector ?? throw new ArgumentNullException(nameof(apiServiceDirector));
        }

        public async Task<TokenResponse> GetToken(
            TokenType tokenType,
            string? clientId = null,
            int tokenLifetime = Constants.AuthServer.DefaultTokenLifetime,
            int sharingDuration = Constants.AuthServer.SharingDuration)
        {
            clientId ??= _options.LastRegisteredClientId;

            var userId = tokenType switch
            {
                TokenType.KamillaSmith => Constants.Users.UserIdKamillaSmith,
                TokenType.MaryMoss => Constants.Users.Energy.UserIdMaryMoss,
                TokenType.JaneWilson => Constants.Users.Banking.UserIdJaneWilson,
                _ => throw new ArgumentException($"{nameof(GetToken)} - Unsupported token type - {tokenType}"),
            };

            var scope = tokenType switch
            {
                TokenType.KamillaSmith => Constants.Scopes.ScopeBanking,
                TokenType.JaneWilson => Constants.Scopes.ScopeBanking,
                TokenType.MaryMoss => Constants.Scopes.ScopeEnergy,
                _ => throw new ArgumentException($"{nameof(GetToken)} - Unsupported token type - {tokenType}"),
            };

            var authService = await new DataHolderAuthoriseService.DataHolderAuthoriseServiceBuilder(_options, _dataHolderParService, _apiServiceDirector, false, _authServerOptions)
                   .WithUserId(userId)
                   .WithScope(scope)
                   .WithClientId(clientId)
                   .WithSharingDuration(sharingDuration)
                   .WithTokenLifetime(tokenLifetime)
                   .BuildAsync();

            (var authCode, _) = await authService.Authorise();

            // User authCode to get tokens
            var tokenResponse = await _dataHolderTokenService.GetResponse(authCode);
            if (tokenResponse == null)
            {
                throw new InvalidOperationException($"{nameof(GetToken)} - TokenResponse is null");
            }

            if (tokenResponse.IdToken == null)
            {
                throw new InvalidOperationException($"{nameof(GetToken)} - Id token is null");
            }

            if (tokenResponse.AccessToken == null)
            {
                throw new InvalidOperationException($"{nameof(GetToken)} - Access token is null");
            }

            if (tokenResponse.RefreshToken == null)
            {
                throw new InvalidOperationException($"{nameof(GetToken)} - Refresh token is null");
            }

            if (tokenResponse.CdrArrangementId == null)
            {
                throw new InvalidOperationException($"{nameof(GetToken)} - CdrArrangementId is null");
            }

            // Return access token
            return tokenResponse;
        }
    }
}
