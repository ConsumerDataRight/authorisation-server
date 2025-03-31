using CdrAuthServer.Configuration;
using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public interface IClientAssertionValidator
    {
        Task<(ValidationResult Result, string? ClientId)> ValidateClientAssertionRequest(
            ClientAssertionRequest clientAssertionRequest,
            ConfigurationOptions configOptions,
            bool isTokenEndpoint);

        Task<(ValidationResult Result, Client? Client)> ValidateClientAssertion(
            string clientAssertion,
            ConfigurationOptions configOptions,
            IList<string>? validAudiences = null);
    }
}
