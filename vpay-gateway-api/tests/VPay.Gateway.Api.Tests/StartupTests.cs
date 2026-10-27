using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Enrollments;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Loaf;
using VPay.Gateway.Common.MemberClaims;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Recon;
using VPay.Gateway.Common.RestPayments;
using VPay.Gateway.Common.UserManagement;
using Xunit;

namespace VPay.Gateway.Api.Tests;

public record App(IConfiguration Configuration, IServiceProvider ServiceProvider);

public class StartupTests
{
    private static App BuildApp()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment == null)
        {
            environment = "LocalDevelopment";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
        }
        var configuration = BuildApplication(environment);
        var serviceProvider = BuildServiceProvider(configuration);

        return new App(configuration, serviceProvider);
    }

    private static IConfiguration BuildApplication(string environment)
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{environment}.json", true)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                // Add any required configuration values here for testing
            })
            .Build();
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var startup = new Startup(configuration);
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        startup.ConfigureServices(collection);
        return collection.BuildServiceProvider();
    }

    [Fact]
    public void ServiceCollection_ShouldHaveRequiredServices()
    {
        var app = BuildApp();
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IUserInfo>());
    }

    [Fact]
    public void AppSettings_ShouldHaveRequiredConfiguration()
    {
        var app = BuildApp();
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<DocumentSystemApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<EnrollmentApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<KuberaApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<LoafApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<MemberClaimsApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<PlatformConfigApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<PreferencingApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<ReconApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<RestPaymentApiHttpClientConfig>>());
        Assert.NotNull(app.ServiceProvider.GetRequiredService<IOptions<UserManagementApiHttpClientConfig>>());
    }

    [Fact]
    public void AppSettings_ShouldLoadSettingsConfiguration()
    {
        var app = BuildApp();
        //Assert.NotEmpty(app.Configuration.GetValue<string>("DocumentSystemApiHttpClientConfig:BaseAddress") ?? string.Empty);
        //Assert.NotEmpty(app.Configuration.GetValue<string>("EnrollmentApiHttpClientConfig:BaseAddress") ?? string.Empty);
        Assert.NotEmpty(app.Configuration.GetValue<string>("KuberaApiHttpClientConfig:BaseAddress") ?? string.Empty);
        Assert.NotEmpty(app.Configuration.GetValue<string>("LoafApiHttpClientConfig:BaseAddress") ?? string.Empty);
        Assert.NotEmpty(app.Configuration.GetValue<string>("MemberClaimsApiHttpClientConfig:BaseAddress") ?? string.Empty);
        //Assert.NotEmpty(app.Configuration.GetValue<string>("PlatformConfigApiHttpClientConfig:BaseAddress") ?? string.Empty);
        //Assert.NotEmpty(app.Configuration.GetValue<string>("PreferencingApiHttpClientConfig:BaseAddress") ?? string.Empty);
        //Assert.NotEmpty(app.Configuration.GetValue<string>("ReconApiHttpClientConfig:BaseAddress") ?? string.Empty);
        Assert.NotEmpty(app.Configuration.GetValue<string>("RestPaymentApiHttpClientConfig:BaseAddress") ?? string.Empty);
        Assert.NotEmpty(app.Configuration.GetValue<string>("UserManagementApiHttpClientConfig:BaseAddress") ?? string.Empty);
    }
}
