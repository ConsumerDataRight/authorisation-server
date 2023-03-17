using CdrAuthServer.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace CdrAuthServer.Validation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ValidateMtlsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateMtlsAttribute>>();
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var configOptions = config.GetConfigurationOptions();

            if (configOptions.OverrideMtlsChecks.Contains(context.HttpContext.Request.Path))
            {
                logger.LogDebug("Overriding mtls check for {path}...", context.HttpContext.Request.Path);
                base.OnActionExecuting(context);
                return;
            }

            if (context.HttpContext?.Request.Headers.TryGetValue(configOptions.ClientCertificateThumbprintHttpHeaderName, out StringValues headerThumbprints) is true)
            {
                if (headerThumbprints.Count > 1)
                {
                    logger.LogError("Multiple client certificate thumbprints found in request header");
                    context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.MTLS_MULTIPLE_THUMBPRINTS);
                }
            }
            else
            {
                logger.LogError("No client certificate found in request header");
                context.Result = ErrorCatalogue.Catalogue().GetErrorResponse(ErrorCatalogue.MTLS_NO_CERTIFICATE);
            }

            // Client certificate ok.
            base.OnActionExecuting(context);
        }
    }
}
