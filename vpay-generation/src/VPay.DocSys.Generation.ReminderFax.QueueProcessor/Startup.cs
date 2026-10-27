using System.IO.Abstractions;
using System.Resources;
﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Extensions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using VPay.Queuing.DependencyInjection;


namespace VPay.DocSys.Generation.RFax.QueueProcessor
{
    public static class Startup
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ApiSettings>>().Value);

            services.Configure<DocSysSettings>(configuration.GetSection("DocSysOperations"))
                .AddTransient(cfg => cfg.GetService<IOptions<DocSysSettings>>().Value);

            services.Configure<ProcessorSettings>(configuration.GetSection("ProcessorSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ProcessorSettings>>().Value);

            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"))
                .AddTransient<IStorageSettings>(cfg => cfg.GetService<IOptions<StorageSettings>>().Value);
            services.Configure<ReminderFaxEmailSettings>(configuration.GetSection("ReminderFaxEmailSettings"))
                .AddTransient(cfg => cfg.GetService<IOptions<ReminderFaxEmailSettings>>().Value);

            services.AddSingleton(new ResourceManager(typeof(BundledFax)));
            services.AddSingleton<FopBaseXsl>();
            services.AddSingleton<EftFaxFopXsl>();
            services.AddSingleton<ReminderFaxBaseXsl>();
            services.AddTransient<IPdfTools, PdfTools>();
            services.AddTransient<ITiffMessageQueuer, TiffMessageQueuer>();
            services.AddTransient<IReminderFaxPdfCreator, ReminderFaxPdfCreator>();
            services.AddTransient<IReminderFaxService, ReminderFaxService>();
            services.AddTransient<IReminderFaxErrorReportBuilder, ReminderFaxErrorReportBuilder>();
            services.AddTransient<IReminderFaxMessageProcessor, ReminderFaxMessageProcessor>();
            services.AddTransient<IReminderFaxMessageQueuer, ReminderFaxMessageQueuer>();
            services.AddTransient<IPayorDetailResolver, PayorDetailResolver>();

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddSingleton<IProcessRunner, CommandLineProcessRunner>();
            services.AddVPayDocSysStorage();

            services.ConfigureDataConnections(configuration);
            services.ConfigureQueueSettings(configuration);
            services.ConfigureHttpClients(configuration);

            services.AddSingleton<MessageProcessor, MessageProcessor>();

            return services;
        }

    }
}
