using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Http.Abstractions;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Constants;
using VPay.DocSys.Parsing.Http.Extensions;
using Xunit;

namespace VPay.DocSys.Parsing.Http.Tests.PlatformConfiguration
{
    public partial class PlatformConfigApiTests
    {
        protected const string DEV_API_NAME = "PlatformConfigurationApi";
        protected const string DEV_API_PROTOCOL = "https";
        protected const string DEV_API_HOST = "platform-configuration.dev.pks.vpayusa.net";
        protected readonly int? DEV_API_PORT = null;
        protected const string LOCAL_API_NAME = "PlatformConfigurationApi";
        protected const string LOCAL_API_PROTOCOL = "http";
        protected const string LOCAL_API_HOST = "localhost";
        protected readonly int? LOCAL_API_PORT = 80;
        protected IServiceProvider CreateServiceProvider<THttpContentObject>(string name, string protocol, string host, int? port, THttpContentObject given)
        {
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                    {"ApiRetrySettings:Count","3"},
                    {"ApiRetrySettings:WaitMs","300"},
                    {"ApiSettings:ServiceSettings:0:ServiceName", name},
                    {"ApiSettings:ServiceSettings:0:Protocol", protocol},
                    {"ApiSettings:ServiceSettings:0:Host", host},
                    {"ApiSettings:ServiceSettings:0:Port", port?.ToString() }
            })
            .Build();

            return new ServiceCollection()
            .AddSingleton(configuration)
            .Configure<ApiSettings>(configuration.GetSection(nameof(ApiSettings)))
                .AddSingleton(s => s.GetRequiredService<IOptions<ApiSettings>>().Value)
            .Configure<ApiRetrySettings>(configuration.GetSection(nameof(ApiRetrySettings)))
                .AddSingleton(s => s.GetRequiredService<IOptions<ApiRetrySettings>>().Value)
            .AddPollyHttpRetryPolicies()

            .AddRefitApiClient<IPlatformConfigurationApi>(
                s => s.GetRequiredService<IOptions<ApiSettings>>().Value[ServiceNames.PLATFORM_CONFIGURATION_API])
            // comment below to send to a local instance of PLATFORM_CONFIGURATION_API
            .AddHttpMessageHandler(
                _ => new MockDelegatingHandler(
                    _ => Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        // newtonsoft.json approach
                        // to use System.Text.Json, do System.Net.Http.Json.JsonContent.Create(given)
                        Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(given)),
                    }))
                ).Services
            // comment above to send to local instance of PLATFORM_CONFIGURATION_API

            .AddHealthChecks()
            .AddRefitApiCheck<IPlatformConfigurationApi>(
                "PlatformConfigurationApiViaRefit",
                async a => {
                    await a.GetAbout();
                    return HealthStatus.Healthy;
                }).Services

            .BuildServiceProvider();
        }
        protected static IPlatformConfigurationApi GetSUT(IServiceCollection services) => GetSUT(services.BuildServiceProvider());
        protected static IPlatformConfigurationApi GetSUT(IServiceProvider provider) => provider.GetRequiredService<IPlatformConfigurationApi>();
        protected readonly Mock<IPlatformConfigurationApi> MockSUT = new();
        protected Faker Fake;

        [Fact]
        public async Task WhenNoBaseAddressShouldThrow()
        {
            // arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ApiRetrySettings:Count","3"},
                    {"ApiRetrySettings:WaitMs","300"},
                    {"ApiSettings:ServiceSettings:0:ServiceName", "PlatformConfigurationApi"},
                })
                .Build();

            // act
            async Task act()
            {
                var sp = new ServiceCollection()
                    .AddSingleton(configuration)
                    .Configure<ApiSettings>(configuration.GetSection(nameof(ApiSettings)))
                    .Configure<ApiRetrySettings>(configuration.GetSection(nameof(ApiRetrySettings)))
                    .AddPollyHttpRetryPolicies()
                    .AddRefitApiClient<IPlatformConfigurationApi>(
                        o => o.GetRequiredService<IOptions<ApiSettings>>().Value["PlatformConfigurationApi"]).Services
                    .BuildServiceProvider();
                var sut = sp.GetService<IPlatformConfigurationApi>();

                await sut.GetAbout();
            }

            // assert
            await Assert.ThrowsAsync<UriFormatException>(act);
        }
    }
}
