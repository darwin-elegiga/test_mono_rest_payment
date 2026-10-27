using System.IO.Abstractions;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.Data.Db2.Abstractions.Trn835Data;
using VPay.Data.Db2.Odbc;
using VPay.DocSys.Common;
using VPay.DocSys.Common.RabbitMQ;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Http.Extensions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using VPay.Queuing.DependencyInjection;

namespace VPay.DocSys.Generation.QueueProcessor.Consolidated
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);
            services.Configure<DocSysSettings>(configuration.GetSection("DocSysOperations"))
                .AddScoped(cfg => cfg.GetService<IOptions<DocSysSettings>>().Value);
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
                .AddTransient<IStorageSettings>(cfg => cfg.GetService<IOptions<StorageSettings>>().Value);
            services.Configure<OdbcConnectionConfig>(configuration.GetSection("Db2"))
                .AddTransient(cfg => cfg.GetService<IOptions<OdbcConnectionConfig>>().Value);
            services.Configure<CustomSendBackOperations>(configuration.GetSection("CustomSendBackOperations"))
                .AddTransient(cfg => cfg.GetService<IOptions<CustomSendBackOperations>>().Value);
            services.Configure<ProcessorSettings>(configuration.GetSection("ProcessorSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ProcessorSettings>>().Value);
            services.Configure<FssiTracSettings>(configuration.GetSection("FssiTracSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<FssiTracSettings>>().Value);
            services.Configure<PythonSettings>(configuration.GetSection("PythonSettings"))
               .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<PythonSettings>>().Value);
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddTransient<IPdfTools, PdfTools>();
            services.AddTransient<IDocSysOperations, DocSysOperations>();
            services.AddTransient<IEobXmlParser, EobXmlParser>();
            services.AddTransient<CleanDocsService, CleanDocsService>();
            services.AddTransient<IPythonRunner, PythonRunner>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddTransient<ICustomSendBackDocumentGeneratorService, CustomSendBackDocumentGeneratorService>();
            services.AddTransient<IEobDocumentGeneratorService, EobDocumentGeneratorService>();
            services.AddTransient<ICoverVPaygeGeneratorService, CoverVPaygeGeneratorService>();
            services.AddTransient<IPacketDocumentGeneratorService, PacketDocumentGeneratorService>();
            services.AddSingleton<ISendBackCheckXmlFileWriter, SendBackCheckXmlFileWriter>();
            services.AddTransient<IFaxArchiveMessageProcessor, FaxArchiveMessageProcessor>();
            services.AddTransient<ITrn835DataRepository, Trn835DataRepository>();
            services.AddTransient<IStpRemittanceRepository, StpRemittanceRepository>();
            services.AddTransient<IRemittancePartnerNetworkApiClient, RemittancePartnerNetworkApiClient>();
            services.AddTransient<IStpRemittanceDocumentService, StpRemittanceDocumentService>();
            services.AddTransient<IStpPaymentRepository, StpPaymentRepository>();
            services.AddTransient<IStpPaymentService, StpPaymentService>();
            services.AddTransient<IMessageRepositoryFactory, MessageRepositoryFactory>();
            services.AddVPayDocSysStorage();
            services.AddVPayFssiTracStorage();

            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients(configuration);
            services.ConfigureRefitHttpClients(configuration);
            services.ConfigureRemittanceApiHttpClient(configuration);

            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }

        public static IServiceCollection ConfigureQueueSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddVPayQueuingServices();

            services.Configure<QueueConfigurations>(configuration)
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<QueueConfigurations>>().Value);

            return services;
        }

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
