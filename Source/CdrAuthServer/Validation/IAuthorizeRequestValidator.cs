using CdrAuthServer.Configuration;
using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public interface IAuthorizeRequestValidator
    {
        Task<AuthorizeRequestValidationResult> Validate(AuthorizeRequest authRequest, ConfigurationOptions configOptions, bool checkGrantExpiredOrUsed = true);

        AuthorizeRequestValidationResult ValidateCallback(AuthorizeRequestValidationResult currentResult, AuthorizeCallbackRequest authCallbackRequest);
    }
}
