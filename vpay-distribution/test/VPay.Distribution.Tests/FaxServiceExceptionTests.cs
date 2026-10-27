using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Common;

using Xunit;

namespace VPay.Distribution.Tests
{
    public class FaxServiceExceptionTests
    {
        private readonly Mock<IFaxFileProcessor> _faxFileProcessor = new Mock<IFaxFileProcessor>();
        private readonly Mock<IRedCardFaxFileProcessor> _redCardFaxFileProcessor = new Mock<IRedCardFaxFileProcessor>();
        private readonly Mock<IFaxMessagePublisher> _faxMessagePublisher = new Mock<IFaxMessagePublisher>();
        private readonly Mock<IEventMessagePublisher> _eventMessagePublisher = new Mock<IEventMessagePublisher>();
        private readonly Mock<IEdi835MessagePublisher> _edi835MessagePublisher = new Mock<IEdi835MessagePublisher>();
        private readonly ILogger<FaxService> _logger = new NullLogger<FaxService>();

        private readonly IFaxService SUT;

        private readonly Random _random = new Random();

        public FaxServiceExceptionTests()
        {
            _faxFileProcessor
                .Setup(_ => _.Process(It.IsAny<DocDistMsg>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("an error occurred."))
                .Verifiable();

            _redCardFaxFileProcessor
                .Setup(_ => _.Process(It.IsAny<DocDistMsg>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("an error occurred."))
                .Verifiable();

            _faxMessagePublisher
                .Setup(_ => _.PublishToTiffQueue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DistTxnData[]>()))
                .Verifiable();

            _eventMessagePublisher.Setup(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()))
                .Verifiable();

            FaxService.Delay = (delayInMs) => Task.Delay(1);

            SUT = new FaxService(
                _faxFileProcessor.Object,
                _redCardFaxFileProcessor.Object,
                _faxMessagePublisher.Object,
                _eventMessagePublisher.Object,
                _edi835MessagePublisher.Object,
                _logger);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowExceptionIfNoCorrespondenceIds(bool isRedCard)
        {
            var data = CreateData(false, isRedCard, 1);

            await Assert.ThrowsAsync<Exception>(() => SUT.DistributeFaxes(data, new CancellationToken()));

            _eventMessagePublisher.Verify(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()),
                Times.Once);

            _faxFileProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                true),
                isRedCard ? Times.Never() : Times.Once());

            _redCardFaxFileProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                true),
                isRedCard ? Times.Once() : Times.Never());

            _faxFileProcessor.VerifyNoOtherCalls();
            _redCardFaxFileProcessor.VerifyNoOtherCalls();
            _faxMessagePublisher.VerifyNoOtherCalls();
            _eventMessagePublisher.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldNotThrowExceptionIfHasCorrespondenceIds(bool isRedCard)
        {
            var data = CreateData(true, isRedCard, 1);

            await SUT.DistributeFaxes(data, new CancellationToken());

            _faxMessagePublisher.Verify(_ => _.PublishToTiffQueue(
                data.Origin,
                data.TPA,
                data.BillingEntity,
                data.BoardUploadTimeStamp,
                It.Is<DistTxnData[]>(__ => __.Length == data.Transactions.Count)),
                Times.Once);

            _eventMessagePublisher.Verify(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()),
                Times.Once);

            _faxFileProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                false),
                isRedCard ? Times.Never() : Times.Once());

            _redCardFaxFileProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                false),
                isRedCard ? Times.Once() : Times.Never());

            _faxFileProcessor.VerifyNoOtherCalls();
            _redCardFaxFileProcessor.VerifyNoOtherCalls();
            _faxMessagePublisher.VerifyNoOtherCalls();
            _eventMessagePublisher.VerifyNoOtherCalls();
        }

        private DocDistMsg CreateData(bool hasCorrespondenceId, bool isRedCard, int transactionCount = 1)
        {
            var data = new DocDistMsg
            {
                BatchSize = transactionCount,
                BillingEntity = "TESTBE",
                Origin = isRedCard ? "RC" : "NONRC",
                TPA = "TESTTPA",
                BoardUploadTimeStamp = DateTime.Now,
                Transactions = new List<DistTxnData>(transactionCount),
            };

            for (int i = 0; i < transactionCount; i++)
            {
                data.Transactions.Add(new DistTxnData
                {
                    TxnId = _random.Next(1000),
                    CorrespondenceId = hasCorrespondenceId ? (long?)_random.Next(10000) : null,
                });
            }

            return data;
        }
    }
}
