using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration;
using VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Tests
{
    public class BaseStatusFileServiceTests : BaseStatsFileTests
    {
        protected Mock<IStatusFilePathHelper> _statusFilePathHelperMock;
        protected Mock<IAuxiliaryFileWriter> _auxiliaryFileWriterMock;
        protected IConfiguration _configuration;
        protected Mock<IDistributionMessageBuilder> _distributionMessageBuilderMock;
        protected Mock<IDocSysConfigService> _docSysConfigMock;
        protected Mock<IDocumentTransactionMessageListBuilder> _documentTransactionMessageListBuilderMock;
        protected Mock<IEdi835TarService> _edi835TarServiceMock;
        protected Mock<IStatusFileTracker> _statusFileTrackerMock;
        protected Mock<ITarService> _tarServiceMock;
        protected Mock<IPlatformConfigurationApi> _platformConfigurationApiMock;
        protected Mock<IPaymentEngineConfirmationFileService> _paymentEngineConfirmationFileServiceMock;
        protected Mock<IRabbitMqPublisherFactory> _rabbitMqPublisherFactory;

        protected static string[] NotRedCardStatusFileData => File.ReadAllLines("edi835Data/B000000001_FSHEQTN001.sts");

        #region Test Data

        public static IEnumerable<object[]> GetStatusFileSummaryTestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Dictionary<string, object>() };
        }

        public static IEnumerable<object[]> NullArgumentsParameters()
        {
            var statusFilePathHelperMock = new Mock<IStatusFilePathHelper>();
            var loggerFactory = SetupLoggerFactory();
            var configuration = SetupConfiguration();
            var docSysConfigMock = new Mock<IDocSysConfigService>();
            var tarServiceMock = new Mock<ITarService>();
            var redCardRepositoryMock = SetupRedCardRepository();
            var fileSystem = new FileSystem();
            var auxiliaryFileWriter = new AuxiliaryFileWriter(loggerFactory.CreateLogger<AuxiliaryFileWriter>(),
                redCardRepositoryMock.Object, fileSystem);
            var documentTransactionMessageListBuilderMock = new Mock<IDocumentTransactionMessageListBuilder>();
            var distributionMessageBuilderMock = new Mock<IDistributionMessageBuilder>();
            var statusFileTrackerMock = SetupStatusFileTracker();
            var edi835TarServiceMock = SetupEdi835TarService();
            var cachedPlatformConfigurationApiMock = SetupPlatformConfigurationApi();
            var paymentEngineConfirmationFileService = SetupPaymentEngineConfirmationFileService();
            var rabbitMqPublisherFactory = new Mock<IRabbitMqPublisherFactory>();

            yield return new object[]
            {
                null, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object, auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, null, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object, auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, null, tarServiceMock.Object, redCardRepositoryMock.Object, auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, null, redCardRepositoryMock.Object, auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, null, auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object, null,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                null, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, null,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                null, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, null, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, null,
                paymentEngineConfirmationFileService.Object, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                null, loggerFactory, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, null, rabbitMqPublisherFactory.Object
            };

            yield return new object[]
            {
                statusFilePathHelperMock.Object, configuration, docSysConfigMock.Object, tarServiceMock.Object, redCardRepositoryMock.Object,
                auxiliaryFileWriter,
                documentTransactionMessageListBuilderMock.Object, distributionMessageBuilderMock.Object,
                statusFileTrackerMock.Object, edi835TarServiceMock.Object, cachedPlatformConfigurationApiMock.Object,
                paymentEngineConfirmationFileService.Object, loggerFactory, null
            };
        }

        #endregion Test Data

        protected override void SetupInitialMocks(string stsFilePath)
        {
            base.SetupInitialMocks(stsFilePath);

            _statusFilePathHelperMock = new Mock<IStatusFilePathHelper>();
            _statusFilePathHelperMock.Setup(x => x.CleanAndValidateStsPath(It.IsAny<string>())).Returns(stsFilePath);

            _configuration = SetupConfiguration();
            _docSysConfigMock = SetupDocSysConfig();
            _tarServiceMock = new Mock<ITarService>();
            RedCardRepositoryMock = SetupRedCardRepository();
            _auxiliaryFileWriterMock = SetupAuxiliaryFileWriter();
            _documentTransactionMessageListBuilderMock = new Mock<IDocumentTransactionMessageListBuilder>();
            _distributionMessageBuilderMock = new Mock<IDistributionMessageBuilder>();
            _statusFileTrackerMock = SetupStatusFileTracker();
            _edi835TarServiceMock = SetupEdi835TarService();
            _platformConfigurationApiMock = SetupPlatformConfigurationApi(null);
            _paymentEngineConfirmationFileServiceMock = SetupPaymentEngineConfirmationFileService();
            _rabbitMqPublisherFactory = new Mock<IRabbitMqPublisherFactory>();

            var publisherMock = new Mock<IRabbitMqPublisher>();
            _rabbitMqPublisherFactory.Setup(x => x.GetPublisher(It.IsAny<QueueConfigurationSettings>(), It.IsAny<ILogger>(), It.IsAny<DeliveryConfirmationMethod>())).Returns(publisherMock.Object);
            publisherMock.Setup(x => x.PublishUsingMessageRepository<DocumentTransactionMessage>(It.IsAny<DocumentTransactionMessage>()));
        }

        protected StatusFileService CreateStatusFileService()
        {
            return new StatusFileService(_statusFilePathHelperMock.Object, _configuration, _docSysConfigMock.Object,
                _tarServiceMock.Object, RedCardRepositoryMock.Object, _auxiliaryFileWriterMock.Object,
                _documentTransactionMessageListBuilderMock.Object, _distributionMessageBuilderMock.Object,
                _statusFileTrackerMock.Object, _edi835TarServiceMock.Object,
                _platformConfigurationApiMock.Object, _paymentEngineConfirmationFileServiceMock.Object,
                LoggerFactory, _rabbitMqPublisherFactory.Object);
        }

        protected static Mock<IAuxiliaryFileWriter> SetupAuxiliaryFileWriter()
        {
            var auxiliaryFileWriterMock = new Mock<IAuxiliaryFileWriter>();
            auxiliaryFileWriterMock.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<StatusFile>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync("test.aux");

            return auxiliaryFileWriterMock;
        }

        protected static IConfiguration SetupConfiguration()
        {
            var serviceSettings = new ServiceSettings
            {
                EndPointFragments = new Dictionary<string, string> { { "EndPointFragments", "" } },
                ServiceName = "FssiTracAPI"
            };
            var apiSettings = new ApiSettings();
            apiSettings.ServiceSettings.Add(serviceSettings);

            return new ConfigurationBuilder()
                .AddJsonFile("statusFileTestData/appsettings.json")
                .Build();
        }

        protected static Mock<IDocSysConfigService> SetupDocSysConfig()
        {
            var docSysConfiguration = new DocSysConfiguration
            {
                EnvironmentSettings = new Dictionary<string, string>
                {
                    ["indemnity_check_type"] = "A"
                }
            };

            var docSysConfigMock = new Mock<IDocSysConfigService>();
            docSysConfigMock.Setup(x => x.ReadConfig(It.IsAny<string>()))
                .Returns(docSysConfiguration);

            return docSysConfigMock;
        }

        protected static Mock<IEdi835TarService> SetupEdi835TarService()
        {
            var edi835TarServiceMock = new Mock<IEdi835TarService>();
            edi835TarServiceMock.Setup(x => x.EnableEdi835Parsing).Returns(true);
            return edi835TarServiceMock;
        }

        protected static Mock<IStatusFileTracker> SetupStatusFileTracker()
        {
            return SetupStatusFileTracker("PaymentStatusFileId", 1L);
        }

        protected static Mock<IStatusFileTracker> SetupStatusFileTracker(string keyName, object keyValue)
        {
            var statusFileTrackerMock = new Mock<IStatusFileTracker>();

            statusFileTrackerMock.Setup(x => x.GetStatusFileSummary(It.IsAny<string>()))
                .Returns(new Dictionary<string, object>
                {
                    [keyName] = keyValue
                });

            return statusFileTrackerMock;
        }

        protected static Mock<IPaymentEngineConfirmationFileService> SetupPaymentEngineConfirmationFileService()
        {
            var paymentEngineConfirmationFileService = new Mock<IPaymentEngineConfirmationFileService>();

            return paymentEngineConfirmationFileService;
        }

        protected static Mock<IPlatformConfigurationApi> SetupPlatformConfigurationApi(string[] tpas = null)
        {
            var platformConfgigurationApiMock = new Mock<IPlatformConfigurationApi>();

            platformConfgigurationApiMock.Setup(m => m.GetCustomSendBackByClient(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<CustomSendBackClientBoardingConfigDto>
                (
                    (tpas ?? new string[] { }).Select(c => new CustomSendBackClientBoardingConfigDto
                    {
                        ClientCode = c,
                        IsCustomSendBack = true
                    })
                    .Concat(new[] { new CustomSendBackClientBoardingConfigDto { ClientCode = "DEFAULT", IsCustomSendBack = false } })
                ));

            return platformConfgigurationApiMock;
        }

        protected static void VerifyAuxiliaryFileWriterWriteCallTimes(Mock<IAuxiliaryFileWriter> auxiliaryFileWriterMock,
            int expectedTimesCalled)
        {
            auxiliaryFileWriterMock.Verify(x => x.Write(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<StatusFile>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Exactly(expectedTimesCalled));
        }
    }
}
