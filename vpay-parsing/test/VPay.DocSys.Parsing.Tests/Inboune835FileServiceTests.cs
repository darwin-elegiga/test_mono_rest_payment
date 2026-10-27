using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using Moq;
using VPay.DocSys.Parsing.Common;
using VPay.Queuing;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Tests.Helpers;

namespace VPay.DocSys.Parsing.Tests
{
    public class inbound_835_file_service_should
    {
        private const string FakeInboundFilePathAndName = "/full/path/to/inbound.835";

        private readonly IInbound835FileService SUT;

        private readonly IFileSystem _fileSystem;
        private readonly Inbound835FileQueueSettings _queueSettings;
        private readonly Inbound835FileServiceSettings _serviceSettings;

        private readonly IMessagePublisherFactory _messagePublisherFactory;
        private readonly IMessagePublisherCache _messagePublisherCache;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly IInbound835FileParser _inbound835FileParser;

        private readonly Mock<IFile> _mockFile;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<IEdi835Repository> _mockEdi835Repository;
        private readonly Mock<IInbound835AcknowledgmentFileProcessor> _mockAcknowledgmentFileProcessor;
        private readonly Mock<IInbound835PaymentPostedFileProcessor> _mockPaymentPostedFileProcessor;
        private readonly Mock<IInbound835RemittanceAdviceFileProcessor> _mockRemittanceAdviceFileProcessor;
        private readonly Mock<IMessagePublisher> _mockMessagePublisher;

        public inbound_835_file_service_should()
        {
            _queueSettings = new Inbound835FileQueueSettings()
            {
                Host = "host_name_1",
                Port = 1,
                UserName = Guid.NewGuid().ToString("N"),
                Password = Guid.NewGuid().ToString("N"),
                VirtualHost = "virtual_host_1",
                QueueName = "queue_1",
            };

            _serviceSettings = new Inbound835FileServiceSettings();

            _fileSystem = new FileSystem();

            _mockFile = new Mock<IFile>();
            _mockFile.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.SetupGet(fs => fs.File).Returns(_mockFile.Object);
            _mockFileSystem.SetupGet(fs => fs.Path).Returns(_fileSystem.Path);


            _mockEdi835Repository = new Mock<IEdi835Repository>();

            _inbound835FileParser = new Inbound835FileParser(_mockFileSystem.Object);

            _mockAcknowledgmentFileProcessor = new Mock<IInbound835AcknowledgmentFileProcessor>();
            _mockAcknowledgmentFileProcessor.Setup(p =>
                p.ProcessFile(It.IsAny<Inbound835FileMessage>(), It.IsAny<CancellationToken>())
                ).Verifiable();

            _mockPaymentPostedFileProcessor = new Mock<IInbound835PaymentPostedFileProcessor>();
            _mockPaymentPostedFileProcessor.Setup(p =>
                p.ProcessFile(It.IsAny<Inbound835FileMessage>(), It.IsAny<CancellationToken>())
                ).Verifiable();

            _mockRemittanceAdviceFileProcessor = new Mock<IInbound835RemittanceAdviceFileProcessor>();
            _mockRemittanceAdviceFileProcessor.Setup(p =>
                p.ProcessFile(It.IsAny<Inbound835FileMessage>(), It.IsAny<CancellationToken>())
                ).Verifiable();
            _mockRemittanceAdviceFileProcessor.Setup(p =>
                p.Send835ToAvaility(It.IsAny<long>(), It.IsAny<CancellationToken>())
                ).Verifiable();

            _mockMessagePublisher = new Mock<IMessagePublisher>(MockBehavior.Strict);

            _messagePublisherFactory = new MockPublisherFactory(_mockMessagePublisher.Object);
            _messagePublisherCache = new MessagePublisherCache(_messagePublisherFactory, NullLogger<MessagePublisherCache>.Instance);
            _messagePublisherService = new MessagePublisherService(_messagePublisherCache);

            _mockMessagePublisher
                .Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<byte?>()));

            SUT = new Inbound835FileService(
                _mockFileSystem.Object,
                _mockEdi835Repository.Object,
                _inbound835FileParser,
                _queueSettings,
                _serviceSettings,
                _messagePublisherService,
                _mockPaymentPostedFileProcessor.Object,
                _mockAcknowledgmentFileProcessor.Object,
                _mockRemittanceAdviceFileProcessor.Object,
                NullLogger<Inbound835FileService>.Instance);
        }

        [Fact]
        public async Task queue_file_for_processing()
        {
            var filePath = FakeInboundFilePathAndName;

            // queue file for processing with message
            var message = new Inbound835FileMessage(filePath);

            await SUT.QueueFileForProcessing(message);

            _mockMessagePublisher
                .Verify(publisher => publisher.Publish(It.Is<string>(m => m.Contains(filePath)), It.Is<byte?>(p => !p.HasValue || p.Value == 0)));

            // verify exception if message is null
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await SUT.QueueFileForProcessing(null as Inbound835FileMessage);
            });
        }

        [Fact]
        public async Task queue_file_for_processing_from_file_path()
        {
            var filePath = FakeInboundFilePathAndName;

            // queue file for processing file path
            await SUT.QueueFileForProcessing(filePath);

            _mockMessagePublisher
                .Verify(publisher => publisher.Publish(It.Is<string>(m => m.Contains(filePath)), It.Is<byte?>(p => !p.HasValue || p.Value == 0)));


            // verify exception if file path is empty
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await SUT.QueueFileForProcessing(string.Empty);
            });

            // verify exception if file path is null
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await SUT.QueueFileForProcessing(null as string);
            });
        }

        [Theory]
        [InlineData("/my/test/path/202010241532.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/202010252455.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/202010262349.835.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/202010271722.835.txt", Inbound835FileType.PaymentPosted)]

        [InlineData("/my/test/path/ERA_AEG_1013080894_00000000000896166662_20201027190354.835", Inbound835FileType.RemittanceAdvice)]
        [InlineData("/my/test/path/ERA_TLC_844449843_00000000000895512000_20201027164939.835", Inbound835FileType.RemittanceAdvice)]

        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452.txt", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/202265068_RC_HBI_HBI0001001_1290001RC2020101453000717A202001323866761_894322699_NEW_20201023121211.txt", Inbound835FileType.Acknowledgment)]

        [InlineData("/my/test/path/954434428_SEMED_HEQTN_HEQTN01001_FKPGY5W_895899215_NEW_20201027042249.ack", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/990107330_IC_SWK_SWKPF01001_A0007182412MEMAX07_896198860_NEW_20201027123147.ack", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/990373279_RC_AEG_AEG0001007_1305001AE2020102300024858A202001332990911_895476039_NEW_20201026151310.ack", Inbound835FileType.Acknowledgment)]
        [InlineData("/my/test/path/990373279_RC_AEG_AEG0001007_1305001AE2020102300024858A202001332990911_895476039_NEW_20201026151310XXX.ack", Inbound835FileType.Acknowledgment)]

        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_RI_20201026143452.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/990373279_RC_AEG_AEG0001007_1305001AE2020102300024858A202001332990911_895476039_RI_20201026151310.txt", Inbound835FileType.PaymentPosted)]
        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452X.txt", Inbound835FileType.PaymentPosted)]

        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452.xxx", Inbound835FileType.Unknown)]
        [InlineData("/my/test/path/561479712_RC_TLC_TLC0001866_1094001LO2020102699019250A202001333306411_895505346_NEW_20201026143452", Inbound835FileType.Unknown)]
        public async Task call_the_appropriate_file_processor(string filePath, Inbound835FileType expectedFileType)
        {
            // verify mapping of filePath to Inbound835FileType
            var inbound835FileType = _inbound835FileParser.GetFileType(filePath);
            inbound835FileType.Should().Be(expectedFileType);

            // create message with file path
            var message = new Inbound835FileMessage(filePath);

            if(inbound835FileType == Inbound835FileType.Unknown)
            {
                // verify not supported exception 
                await Assert.ThrowsAsync<NotSupportedException>(async () =>
                {
                    await SUT.ProcessFile(message);
                });
            }
            else
            {
                // process file using message
                await SUT.ProcessFile(message);

                // verify
                if(expectedFileType == Inbound835FileType.Acknowledgment)
                {
                    _mockAcknowledgmentFileProcessor.Verify(p => p.ProcessFile(It.Is<Inbound835FileMessage>(m => object.ReferenceEquals(m, message)), It.IsAny<CancellationToken>()));
                }
                else if(expectedFileType == Inbound835FileType.PaymentPosted)
                {
                    _mockPaymentPostedFileProcessor.Verify(p => p.ProcessFile(It.Is<Inbound835FileMessage>(m => object.ReferenceEquals(m, message)), It.IsAny<CancellationToken>()));
                }
                else if(expectedFileType == Inbound835FileType.RemittanceAdvice)
                {
                    _mockRemittanceAdviceFileProcessor.Verify(p => p.ProcessFile(It.Is<Inbound835FileMessage>(m => object.ReferenceEquals(m, message)), It.IsAny<CancellationToken>()));
                }
                _mockAcknowledgmentFileProcessor.VerifyNoOtherCalls();
                _mockPaymentPostedFileProcessor.VerifyNoOtherCalls();
                _mockRemittanceAdviceFileProcessor.VerifyNoOtherCalls();
                _mockEdi835Repository.VerifyNoOtherCalls();


                // process file using file path
                await SUT.ProcessFile(filePath);

                // verify
                if(expectedFileType == Inbound835FileType.Acknowledgment)
                {
                    _mockAcknowledgmentFileProcessor.Verify(p => p.ProcessFile(It.Is<Inbound835FileMessage>(m => string.Equals(filePath, m.FilePath)), It.IsAny<CancellationToken>()));
                }
                else if(expectedFileType == Inbound835FileType.PaymentPosted)
                {
                    _mockPaymentPostedFileProcessor.Verify(p => p.ProcessFile(It.Is<Inbound835FileMessage>(m => string.Equals(filePath, m.FilePath)), It.IsAny<CancellationToken>()));
                }
                else if(expectedFileType == Inbound835FileType.RemittanceAdvice)
                {
                    _mockRemittanceAdviceFileProcessor.Verify(p => p.ProcessFile(It.Is<Inbound835FileMessage>(m => string.Equals(filePath, m.FilePath)), It.IsAny<CancellationToken>()));
                }
                _mockAcknowledgmentFileProcessor.VerifyNoOtherCalls();
                _mockPaymentPostedFileProcessor.VerifyNoOtherCalls();
                _mockRemittanceAdviceFileProcessor.VerifyNoOtherCalls();
                _mockEdi835Repository.VerifyNoOtherCalls();
            }
        }

        [Fact]
        public async Task call_remittance_advice_file_processor_send_to_availity()
        {
            const long transactionId = 999999999;

            await SUT.Send835ToAvaility(transactionId, CancellationToken.None);

            _mockRemittanceAdviceFileProcessor.Verify(p => p.Send835ToAvaility(It.Is<long>(n => n == transactionId), It.IsAny<CancellationToken>()));

            _mockAcknowledgmentFileProcessor.VerifyNoOtherCalls();
            _mockPaymentPostedFileProcessor.VerifyNoOtherCalls();
            _mockRemittanceAdviceFileProcessor.VerifyNoOtherCalls();
            _mockEdi835Repository.VerifyNoOtherCalls();
        }
    }
}
