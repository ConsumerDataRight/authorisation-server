namespace CdrAuthServer
{
    using AutoMapper;
    using CdrAuthServer.Domain.Entities;

    public class ServiceMappingProfile : Profile
    {
        private const int AutoMapperMaxDepth = 32;

        public ServiceMappingProfile()
        {
            CreateMap<Grant, Models.Grant>()
                    .Include<Grant, Models.RefreshTokenGrant>()
                    .Include<Grant, Models.AuthorizationCodeGrant>()
                    .Include<Grant, Models.RequestUriGrant>()
                    .Include<Grant, Models.CdrArrangementGrant>()
                    .MaxDepth(AutoMapperMaxDepth);

            CreateMap<Models.RefreshTokenGrant, Grant>().MaxDepth(AutoMapperMaxDepth);
            CreateMap<Models.AuthorizationCodeGrant, Grant>().MaxDepth(AutoMapperMaxDepth);
            CreateMap<Models.RequestUriGrant, Grant>().MaxDepth(AutoMapperMaxDepth);
            CreateMap<Models.CdrArrangementGrant, Grant>().MaxDepth(AutoMapperMaxDepth);

            CreateMap<Grant, Models.RefreshTokenGrant>().MaxDepth(AutoMapperMaxDepth);
            CreateMap<Grant, Models.AuthorizationCodeGrant>().MaxDepth(AutoMapperMaxDepth);
            CreateMap<Grant, Models.RequestUriGrant>().MaxDepth(AutoMapperMaxDepth);
            CreateMap<Grant, Models.CdrArrangementGrant>().MaxDepth(AutoMapperMaxDepth);

            CreateMap<Client, Models.Client>().ReverseMap().MaxDepth(AutoMapperMaxDepth);
            CreateMap<SoftwareProduct, Models.SoftwareProduct>().ReverseMap().MaxDepth(AutoMapperMaxDepth);
        }
    }
}
