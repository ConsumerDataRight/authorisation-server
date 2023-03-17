using CdrAuthServer.Models;

namespace CdrAuthServer.Validation
{
    public class AuthorizeRequestValidationResult : ValidationResult
    {
        public AuthorizationRequestObject ValidatedAuthorizationRequestObject { get; set; }

        public AuthorizeRequestValidationResult(bool isValid) : base(isValid) 
        {
            ValidatedAuthorizationRequestObject = new AuthorizationRequestObject();
        }
    }
}
