using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using Polly.Registry;
using Swashbuckle.AspNetCore.SwaggerUI;
using Vpay.Gateway.Common.Vpay;
using VPay.AspNetCore.HealthChecks;
using VPay.AspNetCore.Logging;
using VPay.AspNetCore.Mvc;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Documents;
using VPay.Gateway.Common.Enrollments;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Identity;
using VPay.Gateway.Common.Kubera;
using VPay.Gateway.Common.Loaf;
using VPay.Gateway.Common.MemberClaims;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Recon;
using VPay.Gateway.Common.RestPayments;
using VPay.Gateway.Common.UserManagement;
using VPay.Gateway.Data.Sql.DependencyInjection;
using VPay.Gateway.Data.Sql.Health;
using VPay.Gateway.Management.Api.Configuration;
using VPay.Gateway.Management.Api.MvcCustomizations;
using VPay.Gateway.Management.Application;
using VPay.MassTransit.Abstractions;
using VPay.MassTransit.DependencyInjection;

namespace VPay.Gateway.Management.Api;

using Hellang.Middleware.ProblemDetails;

[ExcludeFromCodeCoverage]
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        //Recursively get assemblies that are referenced by the API assembly
        var vPayAssemblies = AssemblyScanning.GetVPayAssemblies(typeof(Startup));
        var licenseKey = Configuration["AutoMapper:LicenseKey"];

        services
            .AddAutoMapper(cfg => { cfg.LicenseKey = licenseKey; }, vPayAssemblies)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(vPayAssemblies))
            .AddPolicies()
            .AddVPayMemoryCache();

        services
            .AddResponseCompression()
            .AddHttpContextAccessor()
            .ConfigureOptions<ProblemDetailsOptionsCustomSetup>()
            .AddProblemDetails();

        services
            .AddDocumentsApiClient(config => Configuration.Bind("DocumentSystemApiHttpClientConfig", config))
            .AddEnrollmentsApiClient(Configuration)
            .AddReconApiClient(Configuration)
            .AddKuberaApiClient(config => Configuration.Bind("KuberaApiHttpClientConfig", config))
            .AddPlatformConfigApiClient(config => Configuration.Bind("PlatformConfigApiHttpClientConfig", config))
            .AddLoafApiClient(config => Configuration.Bind("LoafApiHttpClientConfig", config))
            .AddUserManagementApiClient(config => Configuration.Bind("UserManagementApiHttpClientConfig", config))
            .AddPreferencingApiClient(Configuration)
            .AddRestPaymentApiClient(config => Configuration.Bind("RestPaymentApiHttpClientConfig", config))
            .AddMemberClaimsApiClient(config => Configuration.Bind("MemberClaimsApiHttpClientConfig", config))
            .AddVPayApiClient(Configuration)
            .AddGuidewire();

        services
            .AddTransient<IUserInfo, UserInfo>()
            .AddTransient<IPasswordHasher, PasswordHasher>();

        services.ForConfiguration(Configuration)
            .AddSettings<GuidewireReplaySettings>();

        var healthChecks = services
            .AddHealthChecks()
            .AddDocumentsApiHealthCheck()
            .AddEnrollmentsApiHealthCheck()
            .AddReconApiHealthCheck()
            .AddKuberaApiHealthCheck()
            .AddPlatformConfigHealthCheck()
            //.AddExternalClientConfigHealthCheck() // Authenticated endpoint in Platform Config API used by Guidewire
            .AddLoafHealthCheck()
            .AddUserManagementHealthCheck()
            .AddPreferencingApiHealthCheck()
            .AddRestPaymentHealthCheck()
            .AddMemberClaimsHealthCheck()
            .AddCheck<SqlServerSystemHealthCheck>("gateway-sql-server");

        AddDistributedCaching(services, healthChecks);

        services
            .AddControllers(opt =>
            {
                //opt.AddProblemDetailsFilter();
                opt.SetupSlugifyParametersRouteConvention();
            })
            .AddProblemDetailsConventions()
            .AddNewtonsoftJson(options =>
            {
                options.UseCamelCasing(false);

                options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            });

        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblyContaining<Startup>();

        services.EnableRequestMiddlewareValidationLogging();

        services
            .AddOptions()
            .AddSwaggerGenService();

        services
            .AddSql()
            .WithConnectionConfiguration(config => Configuration.Bind("GatewayDatabase", config))
            .WithTypeMappings(vPayAssemblies);

        // RabbitMQ
        var rabbitConfig = new TransportConfig();
        Configuration.Bind("RabbitQueue", rabbitConfig);

        services.UseMassTransitActivityTracking();
        services.UseMassTransit(rabbitConfig, opts =>
        {
            opts.ConfigureJsonDeserializer = ConfigureSerializer;
            opts.ConfigureJsonSerializer = ConfigureSerializer;
        });

        static JsonSerializerSettings ConfigureSerializer(JsonSerializerSettings settings)
        {
            settings.Converters.Add(new SmartEnumValueConverter());
            return settings;
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime)
    {
        app
            .UseResponseCompression()
            .UseAuthentication()
            .UseUserLogging()
            .UseRequestLogging(_ => { })
            .UseProblemDetails()
            .UseSwagger();

        app.UseSwaggerUI(c =>
        {
            const string Title = "Gateway Management";
            c.SwaggerEndpoint("/swagger/v1.0/swagger.json", $"{Title} - v1.0");

            c.SetupVPayDocumentStyles(Title);
        });

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health", new DefaultHealthCheckOptions());
        });

        var logger = loggerFactory.CreateLogger<Startup>();
        appLifetime.ApplicationStarted.Register(() => OnStarted(logger));
        appLifetime.ApplicationStopping.Register(() => OnStopping(logger));
        appLifetime.ApplicationStopped.Register(() => OnStopped(logger));
    }

    private void OnStarted(ILogger logger)
    {
        logger.LogInformation($"Startup occurred for {GetNamespace()}");
    }

    private void OnStopping(ILogger logger)
    {
        logger.LogInformation($"Shutdown occurred for {GetNamespace()}");
    }

    private void OnStopped(ILogger logger)
    {
        logger.LogInformation($"Shutdown completed for {GetNamespace()}");
    }

    private static void OnCacheGetError(Context context, string key, Exception exception)
    {
        if (!context.TryGetLogger(out var logger))
        {
            return;
        }

        logger.LogWarning(exception, "Cannot retrieve '{CacheKey}' from the cache.", key);
    }

    private static void OnCachePutError(Context context, string key, Exception exception)
    {
        if (!context.TryGetLogger(out var logger))
        {
            return;
        }

        logger.LogWarning(exception, "Cannot add '{CacheKey}' to the cache.", key);
    }

    private string GetNamespace() => typeof(Startup).Namespace ?? throw new Exception("Malformed type");

    private void AddDistributedCaching(IServiceCollection services, IHealthChecksBuilder healthChecks)
    {
        var redisConfiguration = Configuration.GetSection("Redis").Get<RedisConfigurationOptions>();
        var isRunningInContainer = bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var runningInContainer)
            && runningInContainer;
        var redisConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            Password = redisConfiguration.Password,
            Ssl = redisConfiguration.Ssl,
            AllowAdmin = redisConfiguration.AllowAdmin,
            ConnectRetry = redisConfiguration.ConnectRetry,
            ConnectTimeout = redisConfiguration.ConnectTimeout,
            ResolveDns = isRunningInContainer,
            AbortOnConnectFail = false
        };

        foreach (var host in redisConfiguration.Hosts)
        {
            redisConfigurationOptions.EndPoints.Add(host);
        }

        services.AddStackExchangeRedisCache(options => options.ConfigurationOptions = redisConfigurationOptions);

        // make sure we add a health check for this.  We'll just set it to "degraded" if it fails
        // because we're going to make sure the code will work whether we can connect or not
        healthChecks.AddRedis(redisConfigurationOptions.ToString(), "gateway-redis", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

        // setup the resiliency policies
        services.AddSingleton<IAsyncCacheProvider<long>>(serviceProvider => serviceProvider
            .GetRequiredService<IDistributedCache>()
            .AsAsyncCacheProvider<string>()
            .WithSerializer(new LongCacheSerializer()));

        services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>(serviceProvider =>
        {
            var registry = serviceProvider.GetService<IPolicyRegistry<string>>();
            registry ??= new PolicyRegistry();

            registry.Add(
                CachingPolicies.TransactionIdCachePolicy,
                Policy.CacheAsync(
                    serviceProvider.GetRequiredService<IAsyncCacheProvider<long>>(),
                    TimeSpan.FromHours(1),
                    onCacheGet: (_, _) => { },
                    onCacheMiss: (_, _) => { },
                    onCachePut: (_, _) => { },
                    onCacheGetError: OnCacheGetError,
                    onCachePutError: OnCachePutError));

            return (PolicyRegistry)registry;
        });
    }
}
