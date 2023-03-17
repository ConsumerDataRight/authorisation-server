using CdrAuthServer.Configuration;
using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public interface IClientAssertionValidator
    {
        Task<(ValidationResult, string? clientId)> ValidateClientAssertionRequest(
            ClientAssertionRequest clientAssertionRequest,
            ConfigurationOptions configOptions,
            bool isTokenEndpoint);

        Task<(ValidationResult, Client?)> ValidateClientAssertion(
            string clientAssertion,
            ConfigurationOptions configOptions,
            IList<string>? validAudiences = null);
    }
}
