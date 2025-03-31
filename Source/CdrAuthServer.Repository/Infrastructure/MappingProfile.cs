namespace CdrAuthServer.Repository.Infrastructure
{
    using System.Data;
    using System.Reflection;
    using AutoMapper;
    using CdrAuthServer.Repository.Entities;
    using Newtonsoft.Json;
    using DomainEntities = CdrAuthServer.Domain.Entities;

    public class MappingProfile : Profile
    {
        private const char MultiStringSeparator = ';';

        public MappingProfile()
        {
            CreateMap<Client, DomainEntities.Client>()
            .AfterMap(MapEntityClientClaimsToDomainClient);

            CreateMap<DomainEntities.Client, Client>()
                .ForMember(dest => dest.ClientClaims, opt => opt.Ignore())
                .AfterMap(MapDomainClientToEntityClientClaims);

            CreateMap<Grant, DomainEntities.Grant>()
                .ForMember(g => g.Data, cfg => cfg.MapFrom((grant, _) => JsonConvert.DeserializeObject<Dictionary<string, object>>(grant.Data)));

            CreateMap<DomainEntities.Grant, Grant>()
                .ForMember(g => g.Data, cfg => cfg.MapFrom((grant, _) => JsonConvert.SerializeObject(grant.Data)));

            CreateMap<SoftwareProduct, DomainEntities.SoftwareProduct>().ReverseMap();

            CreateMap<Client, Client>()
                .ForMember(dest => dest.ClientId, cfg => cfg.Ignore())
                .ForMember(dest => dest.ClientIdIssuedAt, cfg => cfg.Ignore());
        }

        private static PropertyInfo? GetProperyByAttributeNameOrProperyName(object obj, string flagName)
        {
            // get the type:
            var objType = obj.GetType();

            // iterate the properties
            var prop = (from property in objType.GetProperties()
                        from attrib in property.GetCustomAttributes(typeof(JsonPropertyAttribute), false).Cast<JsonPropertyAttribute>()
                        where attrib.PropertyName == flagName
                        select property).FirstOrDefault();

            if (prop == null)
            {
                // we cannot find a property by attribute name so find by property name
                return obj.GetType().GetProperty(flagName);
            }

            return prop;
        }

        public static void MapEntityClientClaimsToDomainClient(Client source, Domain.Entities.Client destination)
        {
            var clientCliams = source.ClientClaims;

            if (clientCliams == null)
            {
                return;
            }

            // map claims to properties on client
            foreach (var item in clientCliams)
            {
                var destionationProperty = GetProperyByAttributeNameOrProperyName(destination, item.Type);

                if (destionationProperty?.PropertyType == typeof(IEnumerable<string>))
                {
                    // split the semicolon separate strings into IEnumerable<string>
                    var splitString = item.Value.Split(MultiStringSeparator, StringSplitOptions.None);

                    destionationProperty.SetValue(destination, splitString, null);
                    continue;
                }

                destionationProperty?.SetValue(destination, item.Value, null);
            }
        }

        public static void MapDomainClientToEntityClientClaims(Domain.Entities.Client source, Client destination)
        {
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

            var sourceProperties = source.GetType().GetProperties(bindingAttr);

            // the properties that are there already on entity as considered ignore list
            // so we can get the claims and we store them as claims
            List<string> ignoreList = destination.GetType().GetProperties(bindingAttr).Select(p => p.Name).ToList();

            var clientClaimsCollection = new List<ClientClaims>();
            foreach (var item in sourceProperties)
            {
                if (item?.Name != null && !ignoreList.Contains(item.Name))
                {
                    string? value = null;

                    // IEnumerable<string>
                    if (item.PropertyType == typeof(IEnumerable<string>))
                    {
                        var values = item.GetValue(source) as IEnumerable<string>;
                        if (values != null)
                        {
                            // split the semicolon separate strings into IEnumberable<string>
                            var joinedString = string.Join(MultiStringSeparator, values);
                            value = joinedString;
                        }
                    }

                    // string type
                    if (item.PropertyType == typeof(string))
                    {
                        value = item.GetValue(source)?.ToString();
                    }

                    // we save the claim only, if it has a value.
                    if (!string.IsNullOrEmpty(value))
                    {
                        var clientCliam = new ClientClaims
                        {
                            ClientId = source.ClientId,

                            // get the jsonproperty name instead of the property name if it is available.
                            Type = item.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? item.Name ?? string.Empty,
                            Value = value,
                        };

                        clientClaimsCollection.Add(clientCliam);
                    }
                }
            }

            var destinationType = destination.GetType();
            destinationType.GetProperty("ClientClaims")?.SetValue(destination, clientClaimsCollection, null);
        }
    }
}
