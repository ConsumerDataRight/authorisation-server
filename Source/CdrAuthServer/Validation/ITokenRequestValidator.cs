using CdrAuthServer.Configuration;
using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public interface ITokenRequestValidator
    {
        Task<ValidationResult> Validate(string? clientId, TokenRequest tokenRequest, ConfigurationOptions configOptions);
    }
}
