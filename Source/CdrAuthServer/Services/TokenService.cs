﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CdrAuthServer.Configuration;
using CdrAuthServer.Domain;
using CdrAuthServer.Domain.Repositories;
using CdrAuthServer.Extensions;
using CdrAuthServer.IdPermanence;
using CdrAuthServer.Models;
using Jose;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Services
{
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly ITokenRepository _tokenRepository;
        private readonly IConfiguration _configuration;
        private readonly IClientService _clientService;
        private readonly IGrantService _grantService;
        private readonly ICustomerService _customerService;

        public TokenService(
            IConfiguration configuration,
            ILogger<TokenService> logger,
            IClientService clientService,
            IGrantService grantService,
            ITokenRepository tokenRepository,
            ICustomerService customerService)
        {
            _configuration = configuration;
            _logger = logger;
            _tokenRepository = tokenRepository;
            _grantService = grantService;
            _clientService = clientService;
            _customerService = customerService;
        }

        private async Task<string> IssueAccessToken(
            string clientId,
            string subjectId,
            List<string> accountIds,
            string scope,
            string cnf,
            ConfigurationOptions configOptions,
            string? cdrArrangementId = null,
            int? cdrArrangementVersion = null,
            string? authCode = null)
        {
            var client = await _clientService.Get(clientId);
            var claims = new List<Claim>();
            var issuer = configOptions.Issuer;

            claims.Add(new Claim(ClaimNames.Issuer, issuer));
            claims.Add(new Claim(ClaimNames.ClientId, clientId));
            claims.Add(new Claim(ClaimNames.Subject, EncryptSub(subjectId, client)));
            claims.Add(new Claim(ClaimNames.SoftwareId, client.SoftwareId));
            claims.Add(new Claim(ClaimNames.JwtId, Guid.NewGuid().ToString()));
            claims.Add(new Claim(ClaimNames.AuthTime, DateTime.UtcNow.ToEpoch().ToString(), ClaimValueTypes.Integer64));

            // add auth code claim to access token.
            if (authCode != null && authCode.HasValue())
            {
                claims.Add(new Claim(ClaimNames.AuthorizationCode, authCode));
            }

            // Add the scopes as an array.
            claims.AddRange(SetAccessTokenScopes(scope, GrantTypes.AuthCode, configOptions).Split(' ').Select(s =>
                new Claim(ClaimNames.Scope, s)));

            if (!string.IsNullOrEmpty(cdrArrangementId))
            {
                claims.Add(new Claim(ClaimNames.CdrArrangementId, cdrArrangementId));
            }

            if (cdrArrangementVersion.HasValue)
            {
                claims.Add(new Claim(ClaimNames.CdrArrangementVersion, cdrArrangementVersion.Value.ToString(), ClaimValueTypes.Integer32));
            }

            if (!string.IsNullOrEmpty(client.SectorIdentifierUri))
            {
                claims.Add(new Claim(ClaimNames.SectorIdentifierUri, client.SectorIdentifierUri));
            }

            // If there are selected account Ids, add them to the access token.
            if (accountIds != null && accountIds.Any())
            {
                var idPermananceManager = new IdPermanenceManager(_configuration);
                var idParameters = new IdPermanenceParameters
                {
                    SoftwareProductId = client.SoftwareId,
                    CustomerId = subjectId
                };
                claims.AddRange(accountIds.Select(a =>
                    new Claim(ClaimNames.AccountId, idPermananceManager.EncryptId(a, idParameters))));
            }

            return await CreateToken(
                claims,
                "cds-au",
                TokenTypes.AccessToken,
                configOptions.AccessTokenExpirySeconds,
                configOptions,
                cnf: cnf);
        }

        public async Task<string> IssueIdToken(
            string clientId,
            string subjectId,
            ConfigurationOptions configOptions,
            bool encrypt,
            string? state = null,
            string? nonce = null,
            string? authCode = null,
            string? accessToken = null,
            string? authTime = null)
        {
            // Claims collection.
            var claims = new List<Claim>();

            // if nonce was sent, must be mirrored in id token
            if (nonce != null && nonce.HasValue())
            {
                claims.Add(new Claim(ClaimNames.Nonce, nonce));
            }

            // add iat claim
            claims.Add(new Claim(ClaimNames.IssuedAt, DateTime.UtcNow.ToEpoch().ToString(), ClaimValueTypes.Integer64));

            // add at_hash claim
            if (accessToken != null && accessToken.HasValue())
            {
                claims.Add(new Claim(ClaimNames.AccessTokenHash, CreateHashClaimValue(accessToken)));
            }

            // add c_hash claim
            if (authCode != null && authCode.HasValue())
            {
                claims.Add(new Claim(ClaimNames.AuthorizationCodeHash, CreateHashClaimValue(authCode)));
            }

            // add s_hash claim
            if (state != null && state.HasValue())
            {
                claims.Add(new Claim(ClaimNames.StateHash, CreateHashClaimValue(state)));
            }

            // Run the sub claim through the ID Permanence.
            var client = await _clientService.Get(clientId);
            claims.Add(new Claim(ClaimNames.Subject, EncryptSub(subjectId, client)));

            // add auth_time claim
            claims.Add(new Claim(ClaimNames.AuthTime, authTime ?? DateTime.UtcNow.ToEpoch().ToString(), ClaimValueTypes.Integer64));

            // add updated_at claim
            claims.Add(new Claim(ClaimNames.UpdatedAt, DateTime.UtcNow.ToEpoch().ToString(), ClaimValueTypes.Integer64));

            // Add user name claims. These should only be added during a token request, not the authorisation request.
            if (!string.IsNullOrEmpty(accessToken))
            {
                if (configOptions.HeadlessMode)
                {
                    var user = new HeadlessModeUser();
                    claims.Add(new Claim(ClaimNames.Name, user.Subject));
                    claims.Add(new Claim(ClaimNames.FamilyName, user.FamilyName));
                    claims.Add(new Claim(ClaimNames.GivenName, user.GivenName));
                }
                else
                {
                    // User claims look up from from seed data source
                    claims.Add(new Claim(ClaimNames.Name, subjectId));
                    
                    // Get customer login details from seed data file instead
                    var userInfo = await _customerService.Get(subjectId);                    
                    claims.Add(new Claim(ClaimNames.GivenName, userInfo.GivenName));
                    claims.Add(new Claim(ClaimNames.FamilyName, userInfo.FamilyName));
                }
            }

            // Set the acr claim to a LoA of 2 - urn:cds.au:cdr:2
            claims.Add(new Claim(ClaimNames.Acr, configOptions.DefaultAcrValue));

            Microsoft.IdentityModel.Tokens.JsonWebKey? clientJwk = null;
            if (encrypt)
            {
                // Get the client enc jwk.
                var jwks = await _clientService.GetJwks(client);
                clientJwk = jwks.Keys.First(jwk => jwk.Alg == client.IdTokenEncryptedResponseAlg);
            }

            string? encryptedResponseAlg = null;
            string? encryptedResponseEnc = null;
            Microsoft.IdentityModel.Tokens.JsonWebKey? encryptedJwk = null;
            if (encrypt)
            {
                encryptedResponseAlg = client.IdTokenEncryptedResponseAlg ?? Constants.Algorithms.Jwe.Alg.RSAOAEP256;
                encryptedResponseEnc = client.IdTokenEncryptedResponseEnc ?? Constants.Algorithms.Jwe.Enc.A256GCM;
                encryptedJwk = clientJwk;
            }

            _logger.LogDebug("Encrypting id_token: {encrypt}", encrypt);
            _logger.LogDebug("encryptedResponseAlg: {encryptedResponseAlg}", encryptedResponseAlg);
            _logger.LogDebug("encryptedResponseEnc: {encryptedResponseEnc}", encryptedResponseEnc);
            _logger.LogDebug("encryptedJwk: {encryptedJwk}", encryptedJwk);

            return await CreateToken(
                claims,
                clientId,
                TokenTypes.IdToken,
                configOptions.IdTokenExpirySeconds,
                configOptions,
                signingAlg: client.IdTokenSignedResponseAlg ?? Constants.Algorithms.Signing.PS256,
                encryptedResponseAlg: encryptedResponseAlg,
                encryptedResponseEnc: encryptedResponseEnc,
                clientJwk: encryptedJwk);
        }

        private string EncryptSub(string subjectId, Client client)
        {
            var idPermanenceManager = new IdPermanenceManager(_configuration);
            var param = new SubPermanenceParameters()
            {
                SoftwareProductId = client.SoftwareId,
                SectorIdentifierUri = client.SectorIdentifierUri
            };

            return idPermanenceManager.EncryptSub(subjectId, param);
        }

        private static string CreateHashClaimValue(string value)
        {
            var octets = Encoding.ASCII.GetBytes(value);
            var hash = SHA256.Create().ComputeHash(octets);
            return Base64UrlEncoder.Encode(hash[..(hash.Length / 2)]);
        }

        public async Task<TokenResponse> IssueTokens(TokenRequest tokenRequest, string cnf, ConfigurationOptions configOptions)
        {
            if (tokenRequest.grant_type == GrantTypes.ClientCredentials)
            {
                return await GetClientCredentialsTokenResponse(tokenRequest, cnf, configOptions);
            }

            if (tokenRequest.grant_type == GrantTypes.AuthCode)
            {
                return await GetAuthCodeTokenResponse(tokenRequest, cnf, configOptions);
            }

            if (tokenRequest.grant_type == GrantTypes.RefreshToken)
            {
                return await GetRefreshTokenResponse(tokenRequest, cnf, configOptions);
            }

            return new TokenResponse() { Error = new Error(ErrorCodes.UnsupportedGrantType, $"Invalid grant_type: {tokenRequest.grant_type}") };
        }

        private async Task<TokenResponse> GetRefreshTokenResponse(TokenRequest tokenRequest, string cnf, ConfigurationOptions configOptions)
        {
            // Get the refresh token and cdr arrangement grants.
            if (await _grantService.Get(GrantTypes.RefreshToken, tokenRequest.refresh_token, tokenRequest.client_id) is not RefreshTokenGrant refreshTokenGrant)
            {
                throw new InvalidOperationException($"Value is null or empty {nameof(refreshTokenGrant)}");
            }
            if (await _grantService.Get(GrantTypes.CdrArrangement, refreshTokenGrant.CdrArrangementId, tokenRequest.client_id) is not CdrArrangementGrant cdrArrangementGrant)
            {
                throw new InvalidOperationException($"Value is null or empty {nameof(cdrArrangementGrant)}");
            }

            var scope = refreshTokenGrant.Scope;
            if (tokenRequest.scope.HasValue())
            {
                var currentScopes = refreshTokenGrant.Scope.Split(' ');
                var newScopes = tokenRequest.scope.Split(' ');

                // Verify that the client has not requested additional scopes that exceed the original request.
                foreach (var newScope in newScopes)
                {
                    if (!currentScopes.Contains(newScope))
                    {
                        return new TokenResponse()
                        {
                            Error = new Error(ErrorCodes.InvalidScope, "Additional scopes were requested in the refresh_token request")
                        };
                    }
                }

                // Additional scopes were not requested, so return the same or subset of scopes.
                scope = tokenRequest.scope;
            }
            
            List<string> accountIds = GetAccountIdsForRefreshToken(cdrArrangementGrant.Data);

            // Refresh the access_token and id_token.
            var accessToken = await IssueAccessToken(
                refreshTokenGrant.ClientId,
                refreshTokenGrant.SubjectId,                
                accountIds,
                scope,
                cnf,
                configOptions,
                refreshTokenGrant.CdrArrangementId,
                cdrArrangementGrant.Version,
                refreshTokenGrant.AuthorizationCode);

            var idToken = await IssueIdToken(
                refreshTokenGrant.ClientId,
                refreshTokenGrant.SubjectId,
                configOptions,
                configOptions.AlwaysEncryptIdTokens || refreshTokenGrant.ResponseType.IsHybridFlow(),
                accessToken: accessToken,
                authTime: refreshTokenGrant.CreatedAt.ToEpoch().ToString());

            return new TokenResponse()
            {
                IdToken = idToken,
                AccessToken = accessToken,
                RefreshToken = tokenRequest.refresh_token,
                ExpiresIn = configOptions.AccessTokenExpirySeconds,
                TokenType = "Bearer",
                CdrArrangementId = refreshTokenGrant.CdrArrangementId,
                Scope = scope
            };
        }

        private List<string> GetAccountIdsForRefreshToken(IDictionary<string, object> data)
        {            
            if (data.ContainsKey(ClaimNames.AccountId))
            {
                var item = data[ClaimNames.AccountId];

                if (item != null)
                {
                    var accountIds = ((System.Collections.IEnumerable)item);
                    List<string> accountList = new List<string>();

                    foreach (var accountId in accountIds)
                    {                        
                        accountList.Add(accountId.ToString());
                    }                    
                    return accountList;
                }
            }
            return new List<string>();
        }

        private async Task<TokenResponse> GetAuthCodeTokenResponse(TokenRequest tokenRequest, string cnf, ConfigurationOptions configOptions)
        {
            // Get the auth code grant.
            if (await _grantService.Get(GrantTypes.AuthCode, tokenRequest.code, tokenRequest.client_id) is not AuthorizationCodeGrant authCodeGrant)
            {
                throw new InvalidOperationException($"Value is null or empty {nameof(authCodeGrant)}");
            }

            var authRequestObject = JsonConvert.DeserializeObject<AuthorizationRequestObject>(authCodeGrant.Request);
            var sharingDuration = authRequestObject?.Claims.SharingDuration ?? 0;
            var existingCdrArrangementId = authRequestObject?.Claims.CdrArrangementId;
            string? cdrArrangementId = null;
            int? cdrArrangementVersion = null;
            string? refreshToken = null;
            DateTime? expiresAt = null;
            DateTime authTime = DateTime.UtcNow;

            // Set the arrangement expiration.
            if (sharingDuration > 0)
            {
                expiresAt = DateTime.UtcNow.AddSeconds(sharingDuration);
            }

            // Amending consent.
            if (!string.IsNullOrEmpty(existingCdrArrangementId))
            {
                cdrArrangementId = existingCdrArrangementId;
                var cdrArrangementGrant = await _grantService.Get(GrantTypes.CdrArrangement, existingCdrArrangementId, authCodeGrant.ClientId) as CdrArrangementGrant;

                // Retrieve the existing cdr arrangement grant.
                if (cdrArrangementGrant == null)
                {
                    throw new InvalidOperationException($"Value is null or empty {nameof(cdrArrangementGrant)}");
                }

                // Update the cdr arrangement grant.
                cdrArrangementVersion = cdrArrangementGrant.Version++; // Increment the version of the arrangement.
                cdrArrangementGrant.ExpiresAt = expiresAt ?? cdrArrangementGrant.ExpiresAt;
                cdrArrangementGrant.AccountIds = authCodeGrant.GetAccountIds();
                cdrArrangementGrant.Version = cdrArrangementVersion.Value;

                // As the cdr arrangement has been amended the existing refresh token needs to be removed and a new refresh token needs to be issued.
                await _grantService.Delete(authCodeGrant.ClientId, GrantTypes.RefreshToken, cdrArrangementGrant.RefreshToken);

                refreshToken = Guid.NewGuid().ToString();
                var refreshTokenGrant = new RefreshTokenGrant()
                {
                    Key = refreshToken,
                    ClientId = authCodeGrant.ClientId,
                    GrantType = GrantTypes.RefreshToken,
                    CreatedAt = authTime,
                    ExpiresAt = cdrArrangementGrant.ExpiresAt,
                    Scope = authCodeGrant.Scope, // Filtered scopes.
                    SubjectId = authCodeGrant.SubjectId,
                    CdrArrangementId = cdrArrangementId,
                    ResponseType = authRequestObject.ResponseType,
                    AuthorizationCode = tokenRequest.code // Keep track of the original auth code that initiated the request
                };
                await _grantService.Create(refreshTokenGrant);

                // Update the cdr arrangement grant with the new refresh token details.
                cdrArrangementGrant.RefreshToken = refreshToken;
                await _grantService.Update(cdrArrangementGrant);
            }
            else
            {
                cdrArrangementId = Guid.NewGuid().ToString();
                cdrArrangementVersion = 1;

                if (sharingDuration > 0)
                {
                    refreshToken = Guid.NewGuid().ToString();

                    // Create the refresh token grant.
                    var refreshTokenGrant = new RefreshTokenGrant()
                    {
                        Key = refreshToken,
                        ClientId = authCodeGrant.ClientId,
                        GrantType = GrantTypes.RefreshToken,
                        CreatedAt = authTime,
                        ExpiresAt = expiresAt.Value,
                        Scope = authCodeGrant.Scope, // Filtered scopes.
                        SubjectId = authCodeGrant.SubjectId,
                        CdrArrangementId = cdrArrangementId,
                        ResponseType = authRequestObject.ResponseType,
                        AuthorizationCode = tokenRequest.code // Keep track of the original auth code that initiated the request
                    };
                    await _grantService.Create(refreshTokenGrant);
                }

                // Create the cdr arrangement grant.
                var cdrArrangementGrant = new CdrArrangementGrant()
                {
                    Key = cdrArrangementId,
                    ClientId = authCodeGrant.ClientId,
                    GrantType = GrantTypes.CdrArrangement,
                    CreatedAt = authTime,
                    ExpiresAt = (expiresAt == null ? DateTime.UtcNow.AddMinutes(configOptions.AccessTokenExpirySeconds) : expiresAt.Value),
                    Scope = authCodeGrant.Scope, // Filtered scopes.
                    SubjectId = authCodeGrant.SubjectId,
                    AccountIds = authCodeGrant.GetAccountIds(),
                    RefreshToken = refreshToken,
                    Version = cdrArrangementVersion.Value
                };
                await _grantService.Create(cdrArrangementGrant);
            }

            // Delete the auth code grant as it has been used.
            await _grantService.Delete(authCodeGrant.ClientId, GrantTypes.AuthCode, authCodeGrant.Key);

            var accessTokenScopes = GetTokenResponseScopes(tokenRequest.scope, authCodeGrant.Scope);

            // Issue the id_token and access_token.
            var accessToken = await IssueAccessToken(
                authCodeGrant.ClientId,
                authCodeGrant.SubjectId,
                authCodeGrant.GetAccountIds(),
                accessTokenScopes,
                cnf,
                configOptions,
                cdrArrangementId,
                cdrArrangementVersion,
                tokenRequest.code);

            var idToken = await IssueIdToken(
                authCodeGrant.ClientId,
                authCodeGrant.SubjectId,
                configOptions,
                encrypt: configOptions.AlwaysEncryptIdTokens || authRequestObject.IsHybridFlow(),
                authCode: authCodeGrant.Key,
                nonce: authRequestObject.Nonce,
                accessToken: accessToken,
                authTime: authTime.ToEpoch().ToString());

            return new TokenResponse()
            {
                IdToken = idToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = configOptions.AccessTokenExpirySeconds,
                TokenType = "Bearer",
                CdrArrangementId = cdrArrangementId,
                Scope = accessTokenScopes
            };
        }

        private static string? GetTokenResponseScopes(string requestedScope, string allowedScope)
        {
            if (string.IsNullOrEmpty(requestedScope))
            {
                return allowedScope;
            }

            if (requestedScope == allowedScope)
            {
                return allowedScope;
            }

            var requestedScopes = requestedScope.Split(' ');
            var allowedScopes = allowedScope.Split(' ');
            var returnScopes = new StringBuilder();

            // Return the subset of scopes requested.
            foreach (var scope in requestedScopes)
            {
                if (allowedScopes.Contains(scope))
                {
                    returnScopes.Append(scope);
                    returnScopes.Append(' ');
                }
            }

            return returnScopes.ToString().TrimEnd(' ');
        }

        private async Task<TokenResponse> GetClientCredentialsTokenResponse(TokenRequest tokenRequest, string cnf, ConfigurationOptions configOptions)
        {
            return new TokenResponse()
            {
                AccessToken = await IssueAccessTokenForClient(tokenRequest, cnf, configOptions),
                ExpiresIn = configOptions.AccessTokenExpirySeconds,
                TokenType = "Bearer",
            };
        }

        private async Task<string> IssueAccessTokenForClient(TokenRequest tokenRequest, string cnf, ConfigurationOptions configOptions)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimNames.ClientId, tokenRequest.client_id),
                new Claim(ClaimNames.JwtId, Guid.NewGuid().ToString())
            };

            // Add the scopes as an array.
            claims.AddRange(SetAccessTokenScopes(tokenRequest.scope, GrantTypes.ClientCredentials, configOptions).Split(' ').Select(s =>
                new Claim(ClaimNames.Scope, s)));

            return await CreateToken(
                claims,
                configOptions.Issuer,
                TokenTypes.AccessToken,
                configOptions.AccessTokenExpirySeconds,
                configOptions,
                cnf: cnf);
        }

        public async Task<string> CreateToken(
            List<Claim> claims,
            string audience,
            string tokenType,
            int expirySeconds,
            ConfigurationOptions configOptions,
            string signingAlg = Constants.Algorithms.Signing.PS256,
            string? encryptedResponseAlg = null,
            string? encryptedResponseEnc = null,
            Microsoft.IdentityModel.Tokens.JsonWebKey? clientJwk = null,
            string? cnf = null)
        {
            var signingCredentials = await GetSigningCredentials(signingAlg);

            // Add confirmation claim if required.
            if (!string.IsNullOrEmpty(cnf))
            {
                claims.Add(new Claim(
                    "cnf",
                    JsonConvert.SerializeObject(new Dictionary<string, string>
                    {
                        { "x5t#S256", cnf }
                    }),
                    System.IdentityModel.Tokens.Jwt.JsonClaimValueTypes.Json));
            }

            // Create a signed, unencrypted JWT.
            var jwtHeader = new JwtHeader(
                            signingCredentials: signingCredentials,
                            outboundAlgorithmMap: null,
                            tokenType: tokenType);

            var jwtPayload = new JwtPayload(
                issuer: configOptions.Issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddSeconds(expirySeconds),
                issuedAt: DateTime.UtcNow);

            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.WriteToken(jwt);

            _logger.LogInformation("encryptedResponseAlg: {encryptedResponseAlg}", encryptedResponseAlg);
            _logger.LogInformation("encryptedResponseEnc: {encryptedResponseEnc}", encryptedResponseEnc);

            if (string.IsNullOrEmpty(encryptedResponseAlg) || string.IsNullOrEmpty(encryptedResponseEnc))
            {
                _logger.LogInformation("token jwt created for encryptedResponseAlg:{alg} encryptedResponseEnc:{enc}", encryptedResponseAlg, encryptedResponseEnc);
                return token;
            }

            // Create a signed, encrypted JWT.
            var rsaEncryption = GetEncryptionKey(clientJwk);

            try
            {                
                _logger.LogDebug("Encrypting Id Token with Alg {Alg}, Enc {Enc}", encryptedResponseAlg, encryptedResponseEnc);

                // Encode the token and add the kid
                return JWT.Encode(
                    token,
                    rsaEncryption,
                    GetJweAlgorithm(encryptedResponseAlg),
                    GetJweEncryption(encryptedResponseEnc),
                    extraHeaders: new Dictionary<string, object>() {
                        { "kid", clientJwk.Kid }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encrypting Id Token with Alg {alg}, Enc {enc} failed", encryptedResponseAlg, encryptedResponseEnc);
                throw new FormatException("Error encrypting id token jwt", ex);
            }
            finally
            {
                if (rsaEncryption != null)
                {
                    rsaEncryption.Dispose();
                }
            }
        }

        private async Task<SigningCredentials> GetSigningCredentials(
            string signingAlg = Constants.Algorithms.Signing.PS256)
        {
            // ES256.
            if (signingAlg.Equals(Constants.Algorithms.Signing.ES256))
            {
                var es256Cert = await _configuration.GetES256SigningCertificate();
                var ecdsa = es256Cert.GetECDsaPrivateKey();
                var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = es256Cert.Thumbprint };
                return new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256)
                {
                    CryptoProviderFactory = new CryptoProviderFactory()
                };
            }

            // PS256.
            var ps256Cert = await _configuration.GetPS256SigningCertificate();
            return new X509SigningCredentials(ps256Cert, SecurityAlgorithms.RsaSsaPssSha256);
        }

        private static RSA? GetEncryptionKey(Microsoft.IdentityModel.Tokens.JsonWebKey? clientJwk)
        {
            if (clientJwk == null)
            {
                return null;
            }

            var rsaEncryption = RSA.Create(new RSAParameters
            {
                Modulus = Base64Url.Decode(clientJwk.N),
                Exponent = Base64Url.Decode(clientJwk.E),
            });
            return rsaEncryption;
        }

        private static JweAlgorithm GetJweAlgorithm(string clientAlg)
        {
            return clientAlg switch
            {
                Algorithms.Jwe.Alg.RSAOAEP => JweAlgorithm.RSA_OAEP,
                Algorithms.Jwe.Alg.RSAOAEP256 => JweAlgorithm.RSA_OAEP_256,
                _ => throw new ArgumentException($"Client Algorithm {clientAlg} not supported for encryption of Id Token"),
            };
        }

        private static JweEncryption GetJweEncryption(string clientEnc)
        {
            return clientEnc switch
            {
                Algorithms.Jwe.Enc.A128GCM => JweEncryption.A128GCM,
                Algorithms.Jwe.Enc.A192GCM => JweEncryption.A192GCM,
                Algorithms.Jwe.Enc.A256GCM => JweEncryption.A256GCM,
                Algorithms.Jwe.Enc.A128CBCHS256 => JweEncryption.A128CBC_HS256,
                Algorithms.Jwe.Enc.A192CBCHS384 => JweEncryption.A192CBC_HS384,
                Algorithms.Jwe.Enc.A256CBCHS512 => JweEncryption.A256CBC_HS512,
                _ => throw new ArgumentException($"Client Encoding {clientEnc} not supported for encryption of Id Token"),
            };
        }

        private string SetAccessTokenScopes(string scope, string grantType, ConfigurationOptions configOptions)
        {
            var scopes = scope.Split(' ');
            var allowedScopes = grantType == GrantTypes.ClientCredentials ? configOptions.ClientCredentialScopesSupported : configOptions.ScopesSupported;

            return string.Join(" ", scopes.Where(s => allowedScopes.Contains(s)));
        }

        public async Task AddToBlacklist(string id)
        {
            await _tokenRepository.AddToBlacklist(id);
            _logger.LogInformation("Revoked token with id:{id} in repository", id);
        }

        public async Task<bool> IsTokenBlacklisted(string id)
        {
            return await _tokenRepository.IsTokenBlacklisted(id);
        }
    }
}
