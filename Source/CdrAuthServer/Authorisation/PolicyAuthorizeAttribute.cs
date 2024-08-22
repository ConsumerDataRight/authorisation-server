using CdrAuthServer.Infrastructure.Authorisation;
using CdrAuthServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CdrAuthServer.Authorisation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PolicyAuthorizeAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public readonly AuthServerAuthorisationPolicyAttribute policy;

        public PolicyAuthorizeAttribute(AuthServerAuthorisationPolicyAttribute policy)
        {
            this.policy = policy;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authorizationService = context.HttpContext.RequestServices.GetService(typeof(IAuthorizationService)) as IAuthorizationService;
            if (authorizationService == null)
            {
                return;
            }
            var authorizationResult = await authorizationService.AuthorizeAsync(context.HttpContext.User, policy.ToString());

            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger)) as ILogger;

            if (authorizationResult.Succeeded)
                return;

            if (authorizationResult.Failure?.FailedRequirements.Any(r => r.GetType() == typeof(HolderOfKeyRequirement)) is true)
            {
                logger?.LogError("invalid_token - holder of Key check failed");
                context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.AUTHORIZATION_HOLDER_OF_KEY_CHECK_FAILED);
                return;
            }

            if (authorizationResult.Failure?.FailedRequirements.Any(r => r.GetType() == typeof(AccessTokenRequirement)) is true)
            {
                logger?.LogError("invalid_token - Access Token check failed - it has been revoked");
                context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.AUTHORIZATION_ACCESS_TOKEN_REVOKED);
                return;
            }

            if (authorizationResult.Failure?.FailureReasons.Any(r => r.Handler.GetType() == typeof(AccessTokenHandler)) is true)
            {
                var reason = authorizationResult.Failure.FailureReasons.First(r => r.Handler.GetType() == typeof(AccessTokenHandler));
                context.Result = new JsonResult(new Error("invalid_token", reason.Message)) { StatusCode = 401 };
                logger?.LogError("invalid_token - {Message}", reason.Message);
                return;
            }

            logger?.LogError("invalid_token - insufficient_scope");
            context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.AUTHORIZATION_INSUFFICIENT_SCOPE);
            context.Result = new JsonResult(new Error("insufficient_scope")) { StatusCode = 403 };

        }
    }
}
