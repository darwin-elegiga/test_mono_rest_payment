using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.Queuing;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class TarServiceTests : IDisposable
    {
        protected readonly List<string> TestFiles = new List<string>();

        [Fact]
        public void ParseTarAndPublishMessages_WorksAsExpected()
        {
            var loggerFactory = new NullLoggerFactory();
            var configuration = GetConfigurationSettings("tarServiceData", Path.DirectorySeparatorChar + "");
            var publisherMock = new Mock<IRabbitMqPublisher>();
            var messagePublisherServiceMock = GetRabbitMqPublisherFactoryMock(publisherMock);
            var tarService = new TarService(configuration, loggerFactory, messagePublisherServiceMock.Object);
            var messages = GetMessagesFromJsonFile();
            tarService.ParseTarAndPublishMessages(messages, string.Empty, false);
            publisherMock.Verify(x => x.PublishUsingMessageRepository<DocumentTransactionMessage>(It.IsAny<DocumentTransactionMessage>()), Times.Exactly(3));
        }

        [Fact]
        public void ParseTarAndPublishMessages_NoMessagesReturns()
        {
            var loggerFactory = new NullLoggerFactory();
            var configuration = GetConfigurationSettings("tarServiceData", Path.DirectorySeparatorChar + "");
            var publisherMock = new Mock<IRabbitMqPublisher>();
            var messagePublisherServiceMock = GetRabbitMqPublisherFactoryMock(publisherMock);
            var tarService = new TarService(configuration, loggerFactory, messagePublisherServiceMock.Object);
            var messages = new List<DocumentTransactionMessage>();
            tarService.ParseTarAndPublishMessages(messages, string.Empty, false);
            publisherMock.Verify(x => x.PublishUsingMessageRepository<DocumentTransactionMessage>(It.IsAny<DocumentTransactionMessage>()), Times.Never);
        }

        [Fact]
        public void ParseTarAndPublishMessages_NullMessagesThrowsException()
        {
            var loggerFactory = new NullLoggerFactory();
            var configuration = GetConfigurationSettings("tarServiceData", Path.DirectorySeparatorChar + "");
            var publisherMock = new Mock<IRabbitMqPublisher>();
            var messagePublisherServiceMock = GetRabbitMqPublisherFactoryMock(publisherMock);
            var tarService = new TarService(configuration, loggerFactory, messagePublisherServiceMock.Object);
            Assert.Throws<ArgumentNullException>(() => tarService.ParseTarAndPublishMessages(null, string.Empty, false));
            publisherMock.Verify(x => x.PublishUsingMessageRepository<DocumentTransactionMessage>(It.IsAny<DocumentTransactionMessage>()), Times.Never);
        }

        [Fact]
        public void ParseTarAndPublishMessages_TarMissingThrowsException()
        {
            var loggerFactory = new NullLoggerFactory();
            var configuration = GetConfigurationSettings("", Path.DirectorySeparatorChar + "");
            var publisherMock = new Mock<IRabbitMqPublisher>();
            var messagePublisherServiceMock = GetRabbitMqPublisherFactoryMock(publisherMock);
            var tarService = new TarService(configuration, loggerFactory, messagePublisherServiceMock.Object);
            var messages = GetMessagesFromJsonFile();
            Assert.Throws<FileNotFoundException>(() => tarService.ParseTarAndPublishMessages(messages, string.Empty, false));
            publisherMock.Verify(x => x.PublishUsingMessageRepository<DocumentTransactionMessage>(It.IsAny<DocumentTransactionMessage>()), Times.Never);
        }

        public void Dispose()
        {
            if (TestFiles != null && TestFiles.Any())
            {
                foreach (var testFile in TestFiles)
                {
                    if (File.Exists(testFile))
                        File.Delete(testFile);

                    var auxFileName = Path.ChangeExtension(testFile, ".aux");
                    if (File.Exists(auxFileName))
                        File.Delete(auxFileName);
                }
            }
        }

        private static IConfigurationRoot GetConfigurationSettings(string fileHome, string tarFolder)
        {
            var configSettingsDict = new Dictionary<string, string>
            {
                { "StatusFile:FileHome", fileHome },
                { "StatusFile:TarFolder", tarFolder }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configSettingsDict)
                .Build();
        }

        private static Mock<IRabbitMqPublisherFactory> GetRabbitMqPublisherFactoryMock(Mock<IRabbitMqPublisher> publisherMock)
        {
            var rabbitMqPublisherFactoryMock = new Mock<IRabbitMqPublisherFactory>();

            rabbitMqPublisherFactoryMock.Setup(x => x.GetPublisher(It.IsAny<QueueConfigurationSettings>(), It.IsAny<ILogger>(), It.IsAny<DeliveryConfirmationMethod>())).Returns(publisherMock.Object);
            publisherMock.Setup(x => x.PublishUsingMessageRepository<DocumentTransactionMessage>(It.IsAny<DocumentTransactionMessage>()));

            return rabbitMqPublisherFactoryMock;
        }

        private static List<DocumentTransactionMessage> GetMessagesFromJsonFile()
        {
            return JsonConvert.DeserializeObject<List<DocumentTransactionMessage>>(
                    File.ReadAllText(Path.Combine("tarServiceData", "messages.json")));
        }
    }
}
