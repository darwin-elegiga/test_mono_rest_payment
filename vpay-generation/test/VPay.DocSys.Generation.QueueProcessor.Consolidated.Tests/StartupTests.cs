using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VPay.Data.Db2.Abstractions.Trn835Data;
using VPay.DocSys.Common;
using VPay.DocSys.Common.RabbitMQ;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Interfaces;
using VPay.DocSys.Generation.Configs;
using VPay.DocSys.Generation.Data;
using VPay.DocSys.Generation.Http.Abstractions;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Pdf.Tools;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.DocSys.Generation.QueueProcessor.Consolidated.Tests
{
    /// <summary>
    /// Confirm that all controllers will have their arguments injected correctly
    /// </summary>
    public class StartupTests
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public StartupTests()
        {
            // setup mocks

            // setup SUT

            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "FssiTracSettings:Port", "0" } // fssi trac port is set via vault/bamboo
                })
                .Build();
            _serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddLogging(b => b.AddProvider(NullLoggerProvider.Instance))
                .AddServices(_configuration)
                .AddSingleton<QueueProcessorService>()
                .BuildServiceProvider();
        }

        [Fact]
        public void AddServices_ShouldInjectForQueueProcessorService()
        {
            Assert.NotNull(_serviceProvider.GetRequiredService<ApiSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<DocSysSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ProcessorSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<PythonSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStorageSettings>());
            Assert.NotNull(_serviceProvider.GetRequiredService<QueueConfigurations>().DocumentGenerationQueue);
            Assert.NotNull(_serviceProvider.GetRequiredService<CustomSendBackOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IDocSysOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFileSystem>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IQueueService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPdfTools>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IDocSysOperations>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IEobXmlParser>());
            Assert.NotNull(_serviceProvider.GetRequiredService<CleanDocsService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPythonRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IProcessRunner>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ICustomSendBackDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IEobDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ICoverVPaygeGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPacketDocumentGeneratorService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ISendBackCheckXmlFileWriter>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IFaxArchiveMessageProcessor>());
            Assert.NotNull(_serviceProvider.GetRequiredService<ITrn835DataRepository>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStpRemittanceRepository>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStorageManager>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IRemittancePartnerNetworkApiClient>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStpRemittanceDocumentService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStpPaymentRepository>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IStpPaymentService>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IMessageRepositoryFactory>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IRemitPartnerNetworkApi>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPlatformConfigurationApi>());
            Assert.NotNull(_serviceProvider.GetRequiredService<IPaymentRoutingApi>());
        }
        
        [Fact]
        public void HttpClient_ShouldHaveBearerTokenAssigned()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
            { "RemittancePartnerNetworkApiRequestHeaders:ApiToken", "test-token" },
            { "RemittancePartnerNetworkApiRequestHeaders:ConsumerName", "test-consumer" },
            { "RemittancePartnerNetworkApiRequestHeaders:Environment", "test-env" }
                })
                .Build();

            services.AddHttpClient<IRemittancePartnerNetworkApiClient, RemittancePartnerNetworkApiClient>((sp, client) =>
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["RemittancePartnerNetworkApiRequestHeaders:ApiToken"]);
            });

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Act
            var client = httpClientFactory.CreateClient(nameof(IRemittancePartnerNetworkApiClient));

            // Assert
            Assert.NotNull(client.DefaultRequestHeaders.Authorization);
            Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal("test-token", client.DefaultRequestHeaders.Authorization.Parameter);
        }

    }
}
