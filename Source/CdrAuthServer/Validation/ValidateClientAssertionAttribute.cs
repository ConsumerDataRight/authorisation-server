using System.Security.Claims;
using CdrAuthServer.Extensions;
using CdrAuthServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static CdrAuthServer.Domain.Constants;

namespace CdrAuthServer.Validation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ValidateClientAssertionAttribute : ActionFilterAttribute
    {
        private readonly bool _isTokenEndpoint = false;

        public ValidateClientAssertionAttribute(bool isTokenEndpoint = false)
        {
            _isTokenEndpoint = isTokenEndpoint;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var clientAssertionValidator = context.HttpContext.RequestServices.GetRequiredService<IClientAssertionValidator>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateClientAssertionAttribute>>();

            // Basic validation.
            if (!context.HttpContext.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase)
             || string.IsNullOrEmpty(context.HttpContext.Request.ContentType)
             || !context.HttpContext.Request.ContentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("ValidateClientAssertion: Content-Type error");
                context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.INVALID_CLIENT);
                return;
            }

            // Get the client assertion values from the form parameters.
            var clientAssertionRequest = new ClientAssertionRequest()
            {
                ClientId = context.HttpContext.Request.Form[ClaimNames.ClientId],
                ClientAssertionType = context.HttpContext.Request.Form[ClaimNames.ClientAssertionType],
                ClientAssertion = context.HttpContext.Request.Form[ClaimNames.ClientAssertion],
                GrantType = context.HttpContext.Request.Form[ClaimNames.GrantType],
                Scope = context.HttpContext.Request.Form[ClaimNames.Scope],
            };

            var configOptions = config.GetConfigurationOptions(context.HttpContext);
            var (result, clientId) = clientAssertionValidator.ValidateClientAssertionRequest(clientAssertionRequest, configOptions, _isTokenEndpoint).GetAwaiter().GetResult();
            if (!result.IsValid)
            {
                logger.LogError("ValidateClientAssertion: failed. {@Error}", result.Error);
                context.Result = new BadRequestObjectResult(new Error(result.Error ?? string.Empty, result.ErrorDescription));
                return;
            }

            // Set the claims principal.
            var claims = new List<Claim>()
            {
                new(ClaimNames.ClientId, clientId ?? string.Empty),
            };
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "client_assertion", ClaimNames.ClientId, ClaimNames.Scope));

            // Client assertion ok.
            base.OnActionExecuting(context);
        }
    }
}
