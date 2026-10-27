using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var authSettings = configuration.GetSection("JwtAuth").Get<AuthSettings>();
            var key = System.Text.Encoding.ASCII.GetBytes(authSettings.SecretKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    // Validate that JWT was issued by us
                    ValidateIssuer = true,
                    ValidIssuer = authSettings.Issuer,

                    // We don't use the optional "Audience" (aud) claim
                    ValidateAudience = false,

                    // Validate the token expiry
                    RequireExpirationTime = true,
                    ValidateLifetime = true,

                    // Useful if we're dealing w/ a server using the "wrong" time. Since it's us, don't need to allow drift
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }
    }
}
