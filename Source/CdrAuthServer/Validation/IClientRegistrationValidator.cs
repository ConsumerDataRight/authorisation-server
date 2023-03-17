using CdrAuthServer.Configuration;
using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public interface IClientRegistrationValidator
    {
        Task<ValidationResult> Validate(ClientRegistrationRequest clientRegistrationRequest, ConfigurationOptions configOptions);
    }
}