using CdrAuthServer.Authorisation;
using CdrAuthServer.Domain.Extensions;
using CdrAuthServer.Infrastructure;
using CdrAuthServer.Infrastructure.Authorisation;
using CdrAuthServer.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CdrAuthServer.SwaggerFilters
{
    public class AuthorizationOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var typeList = new List<Type>() { typeof(PolicyAuthorizeAttribute), typeof(ValidateMtlsAttribute), typeof(ValidateClientAssertionAttribute), typeof(ServiceFilterAttribute) };
            var relAtts = AttributeExtensions.GetAttributes(typeList, context.MethodInfo, true);

            if (relAtts.Any())
            {
                var authAtt = (PolicyAuthorizeAttribute?)relAtts.FirstOrDefault(attr => attr.GetType() == typeof(PolicyAuthorizeAttribute));
                var mtlsFilterAttr = (ServiceFilterAttribute?)relAtts.FirstOrDefault(attr => attr.GetType() == typeof(ServiceFilterAttribute) && ((ServiceFilterAttribute)attr).ServiceType.Name == nameof(ValidateMtlsAttribute));
                var clientAssAttr = (ValidateClientAssertionAttribute?)relAtts.FirstOrDefault(attr => attr.GetType() == typeof(ValidateClientAssertionAttribute));

                var openApiObj = new OpenApiObject();

                if (authAtt != null)
                {
                    // Get the details of the policy
                    var authPolicy = authAtt.PolicyName.GetPolicy();

                    if (authPolicy != null)
                    {
                        if (authPolicy.HasHolderOfKeyRequirement)
                        {
                            openApiObj["hasHolderOfKeyRequirement"] = new OpenApiBoolean(true);
                        }

                        if (authPolicy.HasAccessTokenRequirement)
                        {
                            openApiObj["hasAccessTokenRequirement"] = new OpenApiBoolean(true);
                        }

                        if (authPolicy.HasMtlsRequirement)
                        {
                            openApiObj["hasMtlsRequirement"] = new OpenApiBoolean(true);
                        }

                        if (!authPolicy.ScopeRequirement.IsNullOrEmpty())
                        {
                            openApiObj["scopeRequirement"] = new OpenApiString(authPolicy.ScopeRequirement);
                        }
                    }
                }

                // Make MTLS requirement checks the same
                if (mtlsFilterAttr != null)
                {
                    openApiObj["hasValidateMtlsAttribute"] = new OpenApiBoolean(true);
                }

                if (clientAssAttr != null)
                {
                    openApiObj["hasValidateClientAssertionAttribute"] = new OpenApiBoolean(true);
                }

                operation.Extensions.Add("x-authorisation-policy", openApiObj);
            }
        }
    }
}
