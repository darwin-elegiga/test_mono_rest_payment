using System;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.DocSys.Generation.Api.Controllers;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Pdf.Tools;
using Xunit;

namespace VPay.DocSys.Generation.Api.Tests
{
    /// <summary>
    /// Confirm that all controllers will have their arguments injected correctly
    /// </summary>
    /// <remarks>AboutController has no arguments</remarks>
    public class StartupTests
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public StartupTests()
        {
            // setup mocks

            // setup SUT
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
                {
                    { "FssiTracSettings:Port", "0" } // fssi trac port is set via vault/bamboo
                })
                .Build();
            var startup = new VPay.DocSys.Generation.Api.Startup(_configuration);
            var collection = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
            startup.ConfigureServices(collection);
            _serviceProvider = collection
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldInjectForAchFeeNoticeSupportController()
            => Assert.NotNull(new AchFeeNoticeSupportController(
                _serviceProvider.GetRequiredService<Common.IEftFaxPdfCreator>()));

        [Fact]
        public void AddServices_ShouldInjectForBundledFaxController()
            => Assert.NotNull(new BundledFaxController(
                _serviceProvider.GetRequiredService<IBundledFaxService>()));

        [Fact]
        public void AddServices_ShouldInjectForBundledFaxSupportController()
            => Assert.NotNull(new BundledFaxSupportController(
                _serviceProvider.GetRequiredService<IBundledFaxService>(),
                _serviceProvider.GetRequiredService<IFileSystem>()));

        [Fact]
        public void AddServices_ShouldInjectForCleanDocsController()
            => Assert.NotNull(new CleanDocsController(
                _serviceProvider.GetRequiredService<CleanDocsService>()));

        //        [Fact]
        //        public void AddServices_ShouldInjectForDocSetsController()
        //            => Assert.NotNull(new DocSetsController(
        //                _serviceProvider.GetRequiredService<Common.IDocSysOperations>()));

        [Fact]
        public void AddServices_ShouldInjectForDropToPrintController()
            => Assert.NotNull(new DropToPrintController(
                _serviceProvider.GetRequiredService<Common.IDropToPrintService>(),
                _serviceProvider.GetRequiredService<ILogger<DropToPrintController>>()));

        [Fact]
        public void AddServices_ShouldInjectForEdi835SupportController()
            => Assert.NotNull(new Edi835SupportController(
                _serviceProvider.GetRequiredService<ILogger<Edi835SupportController>>(),
                _serviceProvider.GetRequiredService<Edi835.IEdi835CreationService>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IEdi835Repository>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IPrfTransactionRepository>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IDistributionRepository>(),
                _serviceProvider.GetRequiredService<IEdi835Service>(),
                _serviceProvider.GetRequiredService<Edi835.Edi835ConfigurationCache>()));

        [Fact]
        public void AddServices_ShouldInjectForEftFaxController()
            => Assert.NotNull(new EftFaxController(
                _serviceProvider.GetRequiredService<IFaxMessageService>(),
                _serviceProvider.GetRequiredService<IFaxMessagePayloadProcessor>(),
                _serviceProvider.GetRequiredService<ITiffMessageQueuer>(),
                _serviceProvider.GetRequiredService<ILogger<EftFaxController>>()));

        [Fact]
        public void AddServices_ShouldInjectForFaxController()
            => Assert.NotNull(new FaxController(
                _serviceProvider.GetRequiredService<Models.QueueConfigurations>(),
                _serviceProvider.GetRequiredService<IQueueService>(),
                _serviceProvider.GetRequiredService<IFaxArchiveMessageProcessor>(),
                _serviceProvider.GetRequiredService<ILogger<FaxController>>()));

        [Fact]
        public void AddServices_ShouldInjectForFssiTracController()
            => Assert.NotNull(new FssiTracController(
                _serviceProvider.GetRequiredService<Storage.IFssiTracManager>()));

        [Fact]
        public void AddServices_ShouldInjectForHealthCheckController()
            => Assert.NotNull(new HealthCheckController(
                _serviceProvider.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()));

        [Fact]
        [Obsolete("Testing an obsolete class")]
        public void AddServices_ShouldInjectForLegacyStorageController()
            => Assert.NotNull(new LegacyStorageController(
                _serviceProvider.GetRequiredService<Storage.IStorageManager>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IDocumentTransactionRepository>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IEdi835Repository>(),
                _serviceProvider.GetRequiredService<ILogger<StorageController>>(),
                _serviceProvider.GetRequiredService<IPaymentRoutingApiClient>()));

        [Fact]
        public void AddServices_ShouldInjectForPdfsController()
            => Assert.NotNull(new PdfsController(
                _serviceProvider.GetRequiredService<IPdfTools>(),
                _serviceProvider.GetRequiredService<IFileSystem>(),
                _serviceProvider.GetRequiredService<ITiffOperations>(),
                _serviceProvider.GetRequiredService<ILogger<PdfsController>>()));

        [Fact]
        public void AddServices_ShouldInjectForReminderFaxController()
            => Assert.NotNull(new ReminderFaxController(
                _serviceProvider.GetRequiredService<IReminderFaxMessageQueuer>(),
                _serviceProvider.GetRequiredService<IReminderFaxService>(),
                _serviceProvider.GetRequiredService<ILogger<ReminderFaxController>>()));

        [Fact]
        public void AddServices_ShouldInjectForReminderFaxSupportController()
            => Assert.NotNull(new ReminderFaxSupportController(
                _serviceProvider.GetRequiredService<IReminderFaxPdfCreator>(),
                _serviceProvider.GetRequiredService<VPay.Data.Db2.Abstractions.IFax>(),
                _serviceProvider.GetRequiredService<IPlatformConfigurationApiClient>(),
                _serviceProvider.GetRequiredService<IParsingApiClient>(),
                _serviceProvider.GetRequiredService<ReminderFaxEmailSettings>(),
                _serviceProvider.GetRequiredService<IGreenArrowApiClient>(),
                _serviceProvider.GetRequiredService<IPayorDetailResolver>()));

        [Fact]
        public void AddServices_ShouldInjectForStorageController()
            => Assert.NotNull(new StorageController(
                _serviceProvider.GetRequiredService<Storage.IStorageManager>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IDocumentTransactionRepository>(),
                _serviceProvider.GetRequiredService<DocSys.Data.IEdi835Repository>(),
                _serviceProvider.GetRequiredService<ILogger<StorageController>>(),
                _serviceProvider.GetRequiredService<IPaymentRoutingApiClient>()));

        [Fact]
        public void AddServices_ShouldInjectForWarrantyVPaygeController()
            => Assert.NotNull(new WarrantyVPaygeController(
                _serviceProvider.GetRequiredService<IFaxMessageService>(),
                _serviceProvider.GetRequiredService<IFaxMessagePayloadProcessor>(),
                _serviceProvider.GetRequiredService<ITiffMessageQueuer>(),
                _serviceProvider.GetRequiredService<ILogger<WarrantyVPaygeController>>()));
    }
}
