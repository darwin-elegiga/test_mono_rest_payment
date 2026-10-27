using System.IO.Abstractions;
using System.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Generation.Edi835.Application;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Http.Extensions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Api
{
    public static class ConfigureDependencyHelpers
    {
        public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<DocSysSettings>(configuration.GetSection("DocSysOperations"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<DocSysSettings>>().Value);

            services.Configure<CustomSendBackOperations>(configuration.GetSection("CustomSendBackOperations"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<CustomSendBackOperations>>().Value);

            services.Configure<DropToPrintSettings>(configuration.GetSection("DropToPrintSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<DropToPrintSettings>>().Value);

            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
                .AddScoped<IStorageSettings>(cfg => cfg.GetService<IOptionsSnapshot<StorageSettings>>().Value);

            services.Configure<FssiTracSettings>(configuration.GetSection("FssiTracSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<FssiTracSettings>>().Value);

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ApiSettings>>().Value);

            services.Configure<FaxBase>(configuration.GetSection("FaxBase"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<FaxBase>>().Value);

            services.Configure<ProcessorSettings>(configuration.GetSection("ProcessorSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ProcessorSettings>>().Value);

            services.Configure<FaxJobSettings>(configuration.GetSection("FaxJobSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<FaxJobSettings>>().Value);

            services.Configure<ReminderFaxEmailSettings>(configuration.GetSection("ReminderFaxEmailSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<ReminderFaxEmailSettings>>().Value);

            services.Configure<PythonSettings>(configuration.GetSection("PythonSettings"))
                .AddScoped(cfg => cfg.GetService<IOptionsSnapshot<PythonSettings>>().Value);

            services.AddTransient<IPdfTools, PdfTools>();
            services.AddTransient<ITiffOperations, TiffOperations>();
            services.AddTransient<IDropToPrintService, DropToPrintService>();
            services.AddTransient<IDocSysOperations, DocSysOperations>();
            services.AddTransient<IEobXmlParser, EobXmlParser>();
            services.AddTransient<IBundledFaxService, BundledFaxService>();
            services.AddTransient<IBundledFaxRepository, BundledFaxRepository>();
            services.AddTransient<CleanDocsService, CleanDocsService>();
            services.AddTransient<IPythonRunner, PythonRunner>();
            services.AddTransient<IRedCardRepository, RedCardRepository>();
            services.AddTransient<ICustomSendBackDocumentGeneratorService, CustomSendBackDocumentGeneratorService>();
            services.AddTransient<IEobDocumentGeneratorService, EobDocumentGeneratorService>();
            services.AddTransient<ICoverVPaygeGeneratorService, CoverVPaygeGeneratorService>();
            services.AddTransient<IPacketDocumentGeneratorService, PacketDocumentGeneratorService>();
            services.AddTransient<IFaxMessageService, FaxMessageService>();
            services.AddTransient<IWarrantyPdfCreator, WarrantyPdfCreator>();
            services.AddTransient<IFaxMessageXmlReader, FaxMessageXmlReader>();
            services.AddTransient<IInventoryDriverRecordMessageProcessor, InventoryDriverRecordMessageProcessor>();
            services.AddTransient<IFaxMessagePayloadProcessor, FaxMessagePayloadProcessor>();
            services.AddTransient<IFaxMessageQueueMessageProcessor, FaxMessageQueueMessageProcessor>();
            services.AddTransient<ITiffMessageQueuer, TiffMessageQueuer>();
            services.AddTransient<ITiffQueueMessageProcessor, TiffQueueMessageProcessor>();
            services.AddTransient<IFaxMessageErrorQueueMessageProcessor, FaxMessageErrorQueueMessageProcessor>();
            services.AddTransient<ITiffErrorQueueMessageProcessor, TiffErrorQueueMessageProcessor>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddTransient<IFaxArchiveMessageProcessor, FaxArchiveMessageProcessor>();
            services.AddTransient<IReminderFaxPdfCreator, ReminderFaxPdfCreator>();
            services.AddTransient<IReminderFaxService, ReminderFaxService>();
            services.AddTransient<IReminderFaxErrorReportBuilder, ReminderFaxErrorReportBuilder>();
            services.AddTransient<IReminderFaxMessageProcessor, ReminderFaxMessageProcessor>();
            services.AddTransient<IReminderFaxMessageQueuer, ReminderFaxMessageQueuer>();
            services.AddTransient<IPayorDetailResolver, PayorDetailResolver>();
            services.AddTransient<IEftFaxPdfCreator, EftFaxPdfCreator>();
            services.AddTransient<IEdi835Service, Edi835Service>();
            services.AddTransient<IStpRemittanceRepository, StpRemittanceRepository>();
            services.AddTransient<IStpRemittanceDocumentService, StpRemittanceDocumentService>();
            services.AddTransient<IStpPaymentService, StpPaymentService>();
            services.AddTransient<IDocumentPaymentTransactionRepository, DocumentPaymentTransactionRepository>();
            services.AddTransient<IDb2TransactionRepository, Db2TransactionRepository>();
            services.AddTransient<IDocumentPaymentTransactionService, DocumentPaymentTransactionService>();

            services.AddEdi835CreationServices(configuration);
            services.AddVPayDocSysStorage();
            services.AddVPayFssiTracStorage();

            services.AddSingleton(new ResourceManager(typeof(BundledFax)));

            services.AddSingleton<FopBaseXsl>();
            services.AddSingleton<ReminderFaxBaseXsl>();
            services.AddSingleton<EftFaxFopXsl>();

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddSingleton<ISendBackCheckXmlFileWriter, SendBackCheckXmlFileWriter>();

            services.AddTransient<IQueueService, QueueService>();

            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients(configuration);
            services.ConfigureRefitHttpClients(configuration);
            services.ConfigureRemittanceApiHttpClient(configuration);

            return services;
        }
    }
}
