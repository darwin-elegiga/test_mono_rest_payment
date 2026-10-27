using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly List<List<string>> _securitySchemas;

        public SecurityRequirementsOperationFilter(List<List<string>> securitySchemas)
        {
            _securitySchemas = securitySchemas;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (GetControllerAndActionAttributes<AllowAnonymousAttribute>(context).Any())
            {
                return;
            }

            var actionAttributes = GetControllerAndActionAttributes<AuthorizeAttribute>(context);

            if (!actionAttributes.Any())
            {
                return;
            }

            if (!operation.Responses.ContainsKey("401"))
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            }

            if (!operation.Responses.ContainsKey("403"))
            {
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
            }

            if (_securitySchemas != null)
            {
                var policies = actionAttributes
                        .Where(a => !string.IsNullOrEmpty(a.Policy))
                        .Select(a => a.Policy);

                foreach (var requirements in _securitySchemas.Where(x => x != null))
                {
                    var secReq = new OpenApiSecurityRequirement();

                    foreach (var schema in requirements)
                    {
                        secReq.Add(new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schema } }, policies.ToList());
                    }

                    operation.Security.Add(secReq);
                }
            }
        }

        public static IEnumerable<T> GetControllerAndActionAttributes<T>(OperationFilterContext context) where T : Attribute
        {
            var controllerAttributes = context.MethodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes<T>();
            var actionAttributes = context.MethodInfo.GetCustomAttributes<T>();

            var result = new List<T>(controllerAttributes);
            result.AddRange(actionAttributes);
            return result;
        }
    }
}
