using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using VPay.AspNetCore.SwashBuckle.HealthChecks;

namespace VPay.Gateway.Management.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerGenService(this IServiceCollection services)
    {
        // Register the Swagger generator, defining one or more Swagger documents
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                c.DescribeAllParametersInCamelCase();

                //Names used here are used in URL for SwaggerUI
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Gateway Management API", Version = "v1.0" });

                c.AddHealthCheckDocument("/health");

                c.IncludeXmlComments();
            });

        services.AddSwaggerGenNewtonsoftSupport();

        services.AddFluentValidationRulesToSwagger();

        return services;
    }
}
