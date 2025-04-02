using CdrAuthServer.Configuration;
using CdrAuthServer.Domain;
using CdrAuthServer.Models;
using System.Security.Claims;

namespace CdrAuthServer.Services
{
    public interface ITokenService
    {
        Task<TokenResponse> IssueTokens(
            TokenRequest tokenRequest,
            string cnf,
            ConfigurationOptions configOptions);

        Task<string> IssueIdToken(
            string clientId,
            string subjectId,
            ConfigurationOptions configOptions,
            string? state = null,
            string? nonce = null,
            string? authCode = null,
            string? accessToken = null,
            string? authTime = null);

        Task<string> CreateToken(
            List<Claim> claims,
            string audience,
            string tokenType,
            int expirySeconds,
            ConfigurationOptions configOptions,
            string signingAlg = Constants.Algorithms.Signing.PS256,
            string? encryptedResponseAlg = null,
            string? encryptedResponseEnc = null,
            Microsoft.IdentityModel.Tokens.JsonWebKey? clientJwk = null,
            string? cnf = null);

        Task AddToBlacklist(string id);

        Task<bool> IsTokenBlacklisted(string id);
    }
}
