using CdrAuthServer.Configuration;
using CdrAuthServer.Models;
using System.IdentityModel.Tokens.Jwt;

namespace CdrAuthServer.Validation
{
    public interface IRequestObjectValidator
    {
        Task<(ValidationResult ValidationResult, AuthorizationRequestObject? AuthorizationRequestObject)> Validate(string clientId, JwtSecurityToken requestObject, ConfigurationOptions configOptions);
    }
}
