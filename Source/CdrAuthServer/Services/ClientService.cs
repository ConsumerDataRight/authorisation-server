namespace CdrAuthServer.Services
{
    using AutoMapper;
    using CdrAuthServer.Domain.Repositories;
    using CdrAuthServer.Exceptions;
    using CdrAuthServer.Models;
    using Microsoft.IdentityModel.Tokens;
    using static CdrAuthServer.Domain.Constants;

    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientService> _logger;
        private readonly IJwksService _jwksService;

        public ClientService(
            ILogger<ClientService> logger,
            IJwksService jwksService,
            IClientRepository clientRepository,
            IMapper mapper)
        {
            _logger = logger;
            _jwksService = jwksService;
            _clientRepository = clientRepository;
            _mapper = mapper;
        }

        public async Task<Client?> Get(string? clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return null;
            }

            var client = await _clientRepository.Get(clientId);
            return _mapper.Map<Client?>(client);  
        }

        public async Task<Client> GetClientBySoftwareProductId(string softwareProductId)
        {
            var client = await _clientRepository.GetBySoftwareProductId(softwareProductId);
            return _mapper.Map<Client>(client);
        }

        public async Task<Client> Create(Client client)
        {
            var entity = _mapper.Map<Domain.Entities.Client>(client);
            var entityClient = await _clientRepository.Create(entity);

            _logger.LogInformation("Create client with id:{id}", entityClient?.ClientId);
            return _mapper.Map<Client>(entityClient);
        }

        public async Task<Client> Update(Client client)
        {
            var entity = _mapper.Map<Domain.Entities.Client>(client);
            var entityClient = await _clientRepository.Update(entity);

            _logger.LogInformation("updated the client repository for client:{id}", entityClient?.ClientId);
            return _mapper.Map<Client>(entityClient);
        }

        public async Task Delete(string clientId)
        {
            await _clientRepository.Delete(clientId);
            _logger.LogInformation("deleted client with id:{id}", clientId);
        }

        public async Task<Microsoft.IdentityModel.Tokens.JsonWebKeySet> GetJwks(Client client)
        {
            if (string.IsNullOrEmpty(client.JwksUri))
            {
                _logger.LogError("JwksUri:{uri} is null or empty", client.JwksUri);
                throw new ClientMetadataException(ClaimNames.JwksUri);
            }

            if (!Uri.IsWellFormedUriString(client.JwksUri, UriKind.Absolute))
            {
                _logger.LogError("JwksUri:{uri} is not a valid absolute URI", client.JwksUri);
                throw new ClientMetadataException(ClaimNames.JwksUri, "Not a valid absolute URI");
            }

            return await _jwksService.GetJwks(new Uri(client.JwksUri));
        }

        public async Task<IList<SecurityKey>> GetSigningKeys(Client client)
        {
            var keys = new List<SecurityKey>();
            var jwks = await GetJwks(client);
            if (jwks != null && jwks.Keys.Any())
            {
                keys.AddRange(jwks.Keys);
            }

            return keys;
        }

    }
}
