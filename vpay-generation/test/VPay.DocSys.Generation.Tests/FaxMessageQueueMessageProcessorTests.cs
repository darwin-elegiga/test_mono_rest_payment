using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Newtonsoft.Json;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;
using VPay.Queuing;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class FaxMessageQueueMessageProcessorTests
    {
        protected readonly Mock<IQueueService> _queueService = new Mock<IQueueService>();
        protected readonly Mock<IFaxMessageService> _faxMessageService = new Mock<IFaxMessageService>();
        protected readonly Mock<IFaxMessageXmlReader> _faxMessageXmlReader = new Mock<IFaxMessageXmlReader>();
        protected readonly Mock<ITiffMessageQueuer> _tiffMessageQueuer = new Mock<ITiffMessageQueuer>();

        protected readonly QueueConfigurations _queueConfigurations;

        protected readonly IFaxMessageQueueMessageProcessor SUT;

        public FaxMessageQueueMessageProcessorTests()
        {
            _queueConfigurations = new QueueConfigurations
            {
                CorrespondenceQueue = new QueueConfigurationSettings
                {
                }
            };

            _queueService.Setup(_ => _.PublishQueueMessage(
                It.IsAny<QueueConfigurationSettings>(),
                It.IsAny<string>(),
                It.IsAny<byte?>()));

            var archiveFile = new Mock<IArchiveFile>();

            //archiveFile.Setup(_ => _.GetFullFilePath())
            //    .Returns("test.zip");

            archiveFile.Setup(_ => _.ArchiveDetails)
                .Returns(new IArchiveDetail[] { new ArchiveDetail { FileDetailType = FileDetailType.FaxDocument } });

            _faxMessageService
                .Setup(_ => _.CreateArchiveForWarrantyXmlString(It.IsAny<string>()))
                .ReturnsAsync(archiveFile.Object);

            _faxMessageService
                .Setup(_ => _.CreateArchiveForEftFaxXmlString(It.IsAny<string>()))
                .ReturnsAsync(archiveFile.Object);

            _faxMessageXmlReader
              .Setup(_ => _.ReadXml(It.IsAny<string>()))
              .Returns(new FaxMessageData(new NotificationRequestXmlDto
              {
                  Card = new CardXmlDto(),
                  Control = new ControlXmlDto
                  {
                      Type = "Type",
                      ClientCode = "Tpa",
                      Application = "Application"
                  },
                  SendTo = new SendToXmlDto()
              })
              {
              });

            SUT = new FaxMessageQueueMessageProcessor(
                _queueConfigurations,
                _queueService.Object,
                _faxMessageService.Object,
                _faxMessageXmlReader.Object,
                _tiffMessageQueuer.Object,
                new NullLogger<FaxMessageQueueMessageProcessor>());
        }

        [Theory]
        [InlineData(null)]
        public async Task ShouldThrowExceptionWhenNullString(string message)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => SUT.ProcessMessageString(message));
        }

        [Theory]
        [InlineData("")]
        public async Task ShouldThrowExceptionWhenEmptyString(string message)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => SUT.ProcessMessageString(message));
        }

        [Theory]
        [MemberData(nameof(InvalidCtorValues))]
        public void ShouldThrowExceptionWhenCtorValuesAreInvalid(
            Type exceptionType,
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            IFaxMessageService faxMessageService,
            IFaxMessageXmlReader faxMessageXmlReader,
            ITiffMessageQueuer tiffMessageQueuer,
            ILogger<FaxMessageQueueMessageProcessor> logger)
        {
            Assert.Throws(exceptionType, () => new FaxMessageQueueMessageProcessor(
                queueConfigurations,
                queueService,
                faxMessageService,
                faxMessageXmlReader,
                tiffMessageQueuer,
                logger));
        }

        public static IEnumerable<object[]> InvalidCtorValues
        {
            get
            {
                var qC = new QueueConfigurations { CorrespondenceQueue = new QueueConfigurationSettings() };
                var qS = new Mock<IQueueService>();
                var pFS = new Mock<IFaxMessageService>();
                var pFXR = new Mock<IFaxMessageXmlReader>();
                var tMQ = new Mock<ITiffMessageQueuer>();
                var l = new NullLogger<FaxMessageQueueMessageProcessor>();

                yield return new object[] { typeof(ArgumentNullException), null, qS.Object, pFS.Object, pFXR.Object, tMQ.Object, l };
                yield return new object[] { typeof(ArgumentException), new QueueConfigurations { CorrespondenceQueue = null }, qS.Object, pFS.Object, pFXR.Object, tMQ.Object, l };
                yield return new object[] { typeof(ArgumentNullException), qC, null, pFS.Object, pFXR.Object, tMQ.Object, l };
                yield return new object[] { typeof(ArgumentNullException), qC, qS.Object, null, pFXR.Object, tMQ.Object, l };
                yield return new object[] { typeof(ArgumentNullException), qC, qS.Object, pFS.Object, null, tMQ.Object, l };
                yield return new object[] { typeof(ArgumentNullException), qC, qS.Object, pFS.Object, pFXR.Object, null, l };
                yield return new object[] { typeof(ArgumentNullException), qC, qS.Object, pFS.Object, pFXR.Object, tMQ.Object, null };
            }
        }

        [Theory]
        [MemberData(nameof(FaxMessageIndices))]
        public async Task ShouldHandleInvalidIndex(FaxMessageIndex index)
        {
            var message = $"{new string(' ', 89)}{(int)index + 1:D6}{new string(' ', 180)}{{ }}";

            await Assert.ThrowsAsync<Exception>(() => SUT.ProcessMessageString(message));

            _queueService.Verify(_ => _.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), 0), Times.Once);
        }

        [Fact]
        public async Task ShouldHandleWarrantyFaxCorrectly()
        {
            var message = $"{new string(' ', 89)}{(int)FaxMessageIndex.CreateFax:D6}{new string(' ', 180)}{JsonConvert.SerializeObject(new FaxMessagePayload {TransactionIds = new List<long> {456L}, VPaygeXmlBase64 = "" })}";

            await SUT.ProcessMessageString(message);

            _faxMessageService.Verify(_ => _.CreateArchiveForWarrantyXmlString(It.IsAny<string>()), Times.Once);

            _faxMessageXmlReader.Verify(_ => _.ReadXml(It.IsAny<string>()), Times.Once);

            _queueService.Verify(_ => _.PublishQueueMessage(
                It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), 0),
                Times.Once);

            _tiffMessageQueuer.Verify(_ => _.QueueTiffMessage(
                It.Is<TiffQueueMessage>(__ => __.ReserveName == FaxReserveName.WARRANTY)),
                Times.Once);
        }

        [Fact]
        public async Task ShouldHandleEftFaxCorrectly()
        {
            var message = $"{new string(' ', 89)}{(int)FaxMessageIndex.CreateEftFax:D6}{new string(' ', 180)}{JsonConvert.SerializeObject(new FaxMessagePayload { VPaygeXmlBase64 = "" })}";

            await SUT.ProcessMessageString(message);

            _faxMessageService.Verify(_ => _.CreateArchiveForEftFaxXmlString(It.IsAny<string>()), Times.Once);

            _faxMessageXmlReader.Verify(_ => _.ReadXml(It.IsAny<string>()), Times.Once);

            _queueService.VerifyNoOtherCalls();

            _tiffMessageQueuer.Verify(_ => _.QueueTiffMessage(
                It.Is<TiffQueueMessage>(__ => __.ReserveName == FaxReserveName.VPFEE)),
                Times.Once);
        }

        public static IEnumerable<object[]> FaxMessageIndices
        {
            get
            {
                foreach (FaxMessageIndex i in Enum.GetValues(typeof(FaxMessageIndex)))
                {
                    yield return new object[] { i };
                }
            }
        }
    }
}
