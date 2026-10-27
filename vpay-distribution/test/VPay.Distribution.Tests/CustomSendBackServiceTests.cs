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
    public class CustomSendBackServiceTests
    {
        private readonly Mock<ICustomSendBackProcessor> _customSendBackProcessor = new Mock<ICustomSendBackProcessor>();
        private readonly Mock<IEventMessagePublisher> _eventMessagePublisher = new Mock<IEventMessagePublisher>();
        private readonly Mock<IEdi835MessagePublisher> _edi835MessagePublisher = new Mock<IEdi835MessagePublisher>();
        private readonly ILogger<CustomSendBackService> _logger = new NullLogger<CustomSendBackService>();

        private readonly ICustomSendBackService SUT;

        private readonly Random _random = new Random();

        public CustomSendBackServiceTests()
        {
            _customSendBackProcessor
                .Setup(_ => _.Process(It.IsAny<DocDistMsg>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DistributionProcessResult("CSB"))
                .Verifiable();

            _eventMessagePublisher.Setup(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()))
                .Verifiable();

            CustomSendBackService.Delay = (delayInMs) => Task.Delay(1);
            SUT = new CustomSendBackService(
                _customSendBackProcessor.Object,
                _eventMessagePublisher.Object,
                _edi835MessagePublisher.Object,
                _logger);
        }

        public static IEnumerable<object[]> InvalidCtorParameters
        {
            get
            {
                var csbp = Mock.Of<ICustomSendBackProcessor>();
                var emp = Mock.Of<IEventMessagePublisher>();
                var edi = Mock.Of<IEdi835MessagePublisher>();
                var l = new NullLogger<CustomSendBackService>();

                yield return new object[] { null, emp, edi, l };
                yield return new object[] { csbp, null, edi, l };
                yield return new object[] { csbp, emp, null, l };
                yield return new object[] { csbp, emp, edi, null };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidCtorParameters))]
        public void ShouldThrowExceptionsForNull(
            ICustomSendBackProcessor csbp,
            IEventMessagePublisher emp,
            IEdi835MessagePublisher edi,
            ILogger<CustomSendBackService> l)
        {
            CustomSendBackService.Delay = (delayInMs) => Task.Delay(1);
            Assert.Throws<ArgumentNullException>(() => new CustomSendBackService(csbp, emp, edi, l));
        }

        [Fact]
        public async Task ShouldPublishMessagesWhenTransactionsAvailable()
        {
            var data = CreateData(_random.Next(1, 10));

            await SUT.DistributeCustomSendBacks(data, new CancellationToken());

            _customSendBackProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _eventMessagePublisher.Verify(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()),
                Times.Once);

            _customSendBackProcessor.VerifyNoOtherCalls();
            _eventMessagePublisher.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPublishEventsEvenIfCallThrowsError()
        {
            var data = CreateData(_random.Next(1, 10));
            _customSendBackProcessor
                .Setup(_ => _.Process(It.IsAny<DocDistMsg>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .Verifiable();

            await Assert.ThrowsAsync<Exception>(() => SUT.DistributeCustomSendBacks(data, new CancellationToken()));

            _customSendBackProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _eventMessagePublisher.Verify(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()),
                Times.Once);

            _customSendBackProcessor.VerifyNoOtherCalls();
            _eventMessagePublisher.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDoNothingIfNoTransactionIds()
        {
            var data = CreateData(0);

            await SUT.DistributeCustomSendBacks(data, new CancellationToken());

            _customSendBackProcessor.VerifyNoOtherCalls();
            _eventMessagePublisher.VerifyNoOtherCalls();
        }

        private DocDistMsg CreateData(int transactionCount = 1)
        {
            var data = new DocDistMsg
            {
                BatchSize = transactionCount,
                BillingEntity = "TESTBE",
                Origin = "TESTORIGIN",
                TPA = "TESTTPA",
                BoardUploadTimeStamp = DateTime.Now,
                Transactions = new List<DistTxnData>(transactionCount),
            };

            for (int i = 0; i < transactionCount; i++)
            {
                data.Transactions.Add(new DistTxnData
                {
                    TxnId = _random.Next(1000),
                    CorrespondenceId = null,
                });
            }

            return data;
        }
    }
}
