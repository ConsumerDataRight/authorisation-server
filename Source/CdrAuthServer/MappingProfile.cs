namespace CdrAuthServer
{
    using AutoMapper;
    using CdrAuthServer.Domain.Entities;

    public class ServiceMappingProfile : Profile
    {
        public ServiceMappingProfile()
        {
            CreateMap<Grant, Models.Grant>()
                    .Include<Grant, Models.RefreshTokenGrant>()
                    .Include<Grant, Models.AuthorizationCodeGrant>()
                    .Include<Grant, Models.RequestUriGrant>()
                    .Include<Grant, Models.CdrArrangementGrant>();

            CreateMap<Models.RefreshTokenGrant, Grant>();
            CreateMap<Models.AuthorizationCodeGrant, Grant>();
            CreateMap<Models.RequestUriGrant, Grant>();
            CreateMap<Models.CdrArrangementGrant, Grant>();

            CreateMap<Grant, Models.RefreshTokenGrant>();
            CreateMap<Grant, Models.AuthorizationCodeGrant>();
            CreateMap<Grant, Models.RequestUriGrant>();
            CreateMap<Grant, Models.CdrArrangementGrant>();

            CreateMap<Client, Models.Client>().ReverseMap();
            CreateMap<SoftwareProduct, Models.SoftwareProduct>().ReverseMap();
        }
    }
}
