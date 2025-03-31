using AutoMapper;
using CdrAuthServer.Domain.Repositories;
using CdrAuthServer.Models;

namespace CdrAuthServer.Services
{
    public class CdrService : ICdrService
    {
        private readonly ICdrRepository cdrRepository;
        private readonly IMapper mapper;

        public CdrService(
            ICdrRepository cdrRepository,
            IMapper mapper)
        {
            this.cdrRepository = cdrRepository;
            this.mapper = mapper;
        }

        public async Task<SoftwareProduct> GetSoftwareProduct(string softwareProductId)
        {
            var entity = await cdrRepository.GetSoftwareProduct(softwareProductId);
            return mapper.Map<SoftwareProduct>(entity);
        }

        public async Task InsertDataRecipients(List<SoftwareProduct> softwareProducts)
        {
            if (softwareProducts.Count > 0)
            {
                var softwareProductList = mapper.Map<List<CdrAuthServer.Domain.Entities.SoftwareProduct>>(softwareProducts);
                await cdrRepository.InsertDataRecipients(softwareProductList);
            }
        }

        public async Task PurgeDataRecipients()
        {
            await cdrRepository.PurgeDataRecipients();
        }
    }
}
