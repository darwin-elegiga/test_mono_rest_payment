using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Odbc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VPay.Data.Db2.Odbc;
using VPay.Data.Db2.Odbc.Helpers;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Storage;
using VPay.Extensions.HealthChecks;
using VPay.Extensions.Ssh;

namespace VPay.DocSys.Generation.Edi835.Worker
{
    public static class Startup
    {
        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            var healthCheckSettings = hostContext.Configuration.GetSection("HealthCheckSettings").Get<HealthCheckSettings>();

            var healthBuilder = services
                .AddWorkerHealthCheckPublisher(opts =>
                {
                    opts.Delay = TimeSpan.FromSeconds(10);  // wait 10 secs
                    opts.Period = TimeSpan.FromMinutes(2);  // check every 2 mins
                    opts.Timeout = TimeSpan.FromMinutes(1); // timeout in 1 min
                })
                .AddHealthChecks();

            services.AddOptions();

            services.AddEdi835CreationServices(hostContext.Configuration);

            services.Configure<StorageSettings>(hostContext.Configuration.GetSection("StorageSettings"))
                .AddScoped<IStorageSettings>(cfg => cfg.GetService<IOptionsSnapshot<StorageSettings>>().Value);

            // Add the queue information for this worker
            var queueSettings = new VPay.Queuing.QueueConfigurationSettings();
            hostContext.Configuration.Bind("Edi835GenerationQueue", queueSettings);

            services.AddTransient<IEdi835GenerationMessageProcessor, Edi835GenerationMessageProcessor>();
            services.AddTransient<IFileTransferService, FileTransferService>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddQuorumQueueHostedService<Edi835GenerationConsumer>(queueSettings);

            services.AddVPayDocSysStorage();

            services.ConfigureDataConnections(hostContext.Configuration);

            healthBuilder
                .AddOdbcCheck(name: $"Db2 Database",
                   connectionStringFactory: provider =>
                       provider.GetService<OdbcConnectionConfig>().GetConnectionString(),
                   connectTimeout: healthCheckSettings.Db2ConnectTimeout,
                   commandTimeout: healthCheckSettings.Db2CommandTimeout);

            services.AddVPaySshClientFactory();
        }
    }
}
