using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.Distribution.Common;
using VPay.DocSys.Common;

namespace VPay.Distribution.Api
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureRemittanceApiHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            // TODO: OAuth settings and client access token management are currently disabled.
            //       Uncomment and configure this block if OAuth authentication is required for the Remittance API.
            //var oauthSettings = configuration.GetSection("RemittancePartnerNetworkApiOAuthSettings").Get<OAuthSettings>();

            //services.AddClientAccessTokenManagement(options =>
            //{
            //    options.Clients.Add("RemittanceIdentityServer", new IdentityModel.Client.ClientCredentialsTokenRequest
            //    {
            //        Address = oauthSettings.IdServerUrl,
            //        ClientId = oauthSettings.ClientId,
            //        ClientSecret = oauthSettings.ClientSecret,
            //        Scope = oauthSettings.Scope,
            //        GrantType = oauthSettings.GrantType
            //    });
            //});

            services.AddHttpClient<IRemittancePartnerNetworkApiClient, RemittancePartnerNetworkApiClient>((sp, _) =>
            {
                _.BaseAddress = sp.GetService<IOptionsMonitor<ApiSettings>>()
                    .CurrentValue["RemittancePartnerNetworkApi"]
                    .GetUri();

                _.DefaultRequestHeaders.Accept.Clear();
                _.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _.DefaultRequestHeaders.Add("consumername",
                    configuration.GetSection("RemittancePartnerNetworkApiRequestHeaders").GetValue<string>("ConsumerName"));
                _.DefaultRequestHeaders.Add("x-upstream-env",
                    configuration.GetSection("RemittancePartnerNetworkApiRequestHeaders").GetValue<string>("Environment"));
                var token = configuration.GetSection("RemittancePartnerNetworkApiRequestHeaders").GetValue<string>("ApiToken");
                _.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });
            //.AddClientAccessTokenHandler("RemittanceIdentityServer");
            return services;
        }
    }
}
