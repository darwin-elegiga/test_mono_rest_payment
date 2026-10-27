using System.Collections.Generic;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using VPay.AspNetCore.SwashBuckle.HealthChecks;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerGenService(this IServiceCollection services)
        {
            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.DescribeAllParametersInCamelCase();

                c.SchemaFilter<SmartEnumSchemaFilter>();

                //Names used here are used in URL for SwaggerUI
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Optum Payment Routing API", Version = "v1.0" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.
                        Enter 'Bearer' [space] and then your token in the text input below.
                        Example: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.OperationFilter<SecurityRequirementsOperationFilter>(new List<List<string>> { new List<string> { "Bearer" } });

                c.AddHealthCheckDocument("/health");

                c.CustomSchemaIds(type => type.FullName.Replace("+", "."));

                c.IncludeXmlComments();
            });

            services.AddFluentValidationRulesToSwagger();

            services.AddSwaggerGenNewtonsoftSupport();

            return services;
        }
    }
}
