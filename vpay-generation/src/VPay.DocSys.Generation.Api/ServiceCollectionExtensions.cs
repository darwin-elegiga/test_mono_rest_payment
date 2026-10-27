using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api
{
    public static class ServiceCollectionExtensions
    {
        //private static readonly ClientCredentialsClientName _remittanceClientName = ClientCredentialsClientName.Parse("RemittanceIdentityServer");

        public static IServiceCollection ConfigureRemittanceApiHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            // TODO: OAuth settings and client access token management are currently disabled.
            //       Uncomment and configure this block if OAuth authentication is required for the Remittance API.
            //var oauthSettings = configuration.GetSection("RemittancePartnerNetworkApiOAuthSettings").Get<OAuthSettings>();

            //services
            //    .AddClientCredentialsTokenManagement()
            //    .AddClient(_remittanceClientName, client =>
            //    {
            //        client.TokenEndpoint = new Uri(oauthSettings.IdServerUrl);
            //        client.ClientId = ClientId.Parse(oauthSettings.ClientId);
            //        client.ClientSecret = ClientSecret.Parse(oauthSettings.ClientSecret);
            //        client.Scope = Scope.Parse(oauthSettings.Scope);
            //    });

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
            //.AddClientCredentialsTokenHandler(_remittanceClientName);
            return services;
        }
    }
}
