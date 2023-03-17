namespace CdrAuthServer.Services
{
    using System.Collections.Generic;
    using AutoMapper;
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Extensions;
    using CdrAuthServer.Models;
    using static CdrAuthServer.Domain.Constants;

    public class GrantService : IGrantService
    {
        private readonly ILogger<GrantService> logger;
        private readonly IGrantRepository grantRepository;
        private readonly IMapper mapper;

        public GrantService(
            ILogger<GrantService> logger,
            IGrantRepository grantRepository,
            IMapper mapper)
        {
            this.logger = logger;
            this.grantRepository = grantRepository;
            this.mapper = mapper;
        }

        public async Task<IList<Grant>> ListForClient(string clientId, string grantType)
        {
            var list = await grantRepository.List(clientId, grantType);
            return mapper.Map<IList<Grant>>(list);
        }

        public async Task<Grant?> Get(string grantType, string key, string? clientId = null)
        {
            var grant = await grantRepository.Get(key);

            if (grant == null && grantType.Equals(GrantTypes.RefreshToken, StringComparison.Ordinal))
            {
                // For backwards compatibility ; 
                var hashedKey = GetHashKey(key);
                grant = await grantRepository.Get(hashedKey);
            }

            if (grant == null)
            {
                logger.LogError("Grant not found for key:{key}", key);
                return null;
            }
            if (grant.GrantType != grantType)
            {
                logger.LogError("Grant not found doesn't match for:{key}", key);
                return null;
            }
            if (!string.IsNullOrEmpty(clientId) && grant.ClientId != clientId)
            {
                logger.LogError("Grant not found doesn't match with clientId:{key}", clientId);
                return null;
            }

            return grantType switch
            {
                GrantTypes.CdrArrangement => mapper.Map<CdrArrangementGrant>(grant),

                GrantTypes.RefreshToken => mapper.Map<RefreshTokenGrant>(grant),

                GrantTypes.AuthCode => mapper.Map<AuthorizationCodeGrant>(grant),

                GrantTypes.RequestUri => mapper.Map<RequestUriGrant>(grant),

                _ => mapper.Map<Grant>(grant)
            };
        }

        // Hash key for migration data look up
        private string GetHashKey(string key)
        {
            return string.IsNullOrEmpty(key) ? string.Empty : $"{key}:{GrantTypes.RefreshToken}".Sha256();
        }

        public async Task<Grant> Create(Grant grant)
        {
            var domain = mapper.Map<Domain.Entities.Grant>(grant);

            var entity = await grantRepository.Create(domain);
            logger.LogInformation("Grant created with key:{key}", entity?.Key);

            return mapper.Map<Grant>(entity);
        }

        public async Task<Grant> Update(Grant grant)
        {
            var domain = mapper.Map<Domain.Entities.Grant>(grant);
            var entity = await grantRepository.Update(domain);

            logger.LogInformation("Grant updated for, key:{key}", entity?.Key);
            return mapper.Map<Grant>(entity);
        }

        public async Task Delete(string? clientId, string grantType, string key)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return;
            }

            // Check that the client owns the grant before deleting it.
            var grant = await Get(grantType, key, clientId);
            if (grant != null)
            {
                await grantRepository.Delete(key);
                logger.LogInformation("Grant deleted with, key:{key}", grant?.Key);
            }
        }
    }
}
