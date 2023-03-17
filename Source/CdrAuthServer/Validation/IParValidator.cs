using CdrAuthServer.Configuration;
using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public interface IParValidator
    {
        Task<(ValidationResult, AuthorizationRequestObject?)> Validate(string clientId, string requestObject, ConfigurationOptions configOptions);
    }
}
