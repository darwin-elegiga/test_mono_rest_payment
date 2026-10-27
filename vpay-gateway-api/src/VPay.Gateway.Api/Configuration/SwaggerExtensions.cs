using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using VPay.Gateway.Api.Authentication;

namespace VPay.Gateway.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerGenService(this IServiceCollection services)
    {
        // Register the Swagger generator, defining one or more Swagger documents
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                // NOTE: We need to replace the '+' character for nested types to fix two errors in the swagger json:
                //      1. $ref values must be RFC3986-compliant percent-encoded URIs
                //      2. Component names can only contain the characters A-Z a-z 0-9 - . _
                c.CustomSchemaIds(x => x.FullName.Replace('+', '_'));

                c.DescribeAllParametersInCamelCase();

                //Names used here are used in URL for SwaggerUI
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Gateway API", Version = "v1.0" });

                c.OperationFilter<SecurityRequirementsOperationFilter>(new List<List<string>> { new List<string> { "Client-Id-Header", "Client-Secret-Header" } });

                c.AddSecurityDefinition("Client-Id-Header", new OpenApiSecurityScheme
                {
                    Description = "This is the Client Id that will be added to the Header. You also need the Client Secret Header in order to have a valid login",
                    Name = "X-Client-Id",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                c.AddSecurityDefinition("Client-Secret-Header", new OpenApiSecurityScheme
                {
                    Description = "This is the Client Secret that will be added to the Header. You also need the Client Id Header in order to have a valid login",
                    Name = "X-Client-Secret",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                // NOTE: Hidden for now because we don't want to display this endpoint for external clients
                // c.AddHealthCheckDocument("/health");

                c.IncludeXmlComments();
            });

        services.AddSwaggerGenNewtonsoftSupport();

        services.AddFluentValidationRulesToSwagger();

        return services;
    }
}
