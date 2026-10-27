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
    public class MailServiceTests
    {
        private readonly Mock<IMailFileProcessor> _mailFileProcessor = new Mock<IMailFileProcessor>();
        private readonly Mock<IEventMessagePublisher> _eventMessagePublisher = new Mock<IEventMessagePublisher>();
        private readonly Mock<IEdi835MessagePublisher> _edi835MessagePublisher = new Mock<IEdi835MessagePublisher>();
        private readonly ILogger<MailService> _logger = new NullLogger<MailService>();

        private readonly IMailService SUT;

        private readonly Random _random = new Random();

        public MailServiceTests()
        {
            // this behavior is here so that we don't change it without deliberation
            _mailFileProcessor
                .Setup(_ => _.Process(It.IsAny<DocDistMsg>(), false))
                .ThrowsAsync(new Exception("ALWAYS CREATE CONTROL FILES FOR MAIL"))
                .Verifiable();

            _mailFileProcessor
                .Setup(_ => _.Process(It.IsAny<DocDistMsg>(), true))
                .ReturnsAsync(new DistributionProcessResult("MAIL"))
                .Verifiable();

            _eventMessagePublisher.Setup(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()))
                .Verifiable();

            MailService.Delay = (delayInMs) => Task.Delay(1);

            SUT = new MailService(
                _mailFileProcessor.Object,
                _eventMessagePublisher.Object,
                _edi835MessagePublisher.Object,
                _logger);
        }

        public static IEnumerable<object[]> InvalidCtorParameters
        {
            get
            {
                var mfp = Mock.Of<IMailFileProcessor>();
                var emp = Mock.Of<IEventMessagePublisher>();
                var edi = Mock.Of<IEdi835MessagePublisher>();
                var l = new NullLogger<MailService>();

                yield return new object[] { null, emp, edi, l };
                yield return new object[] { mfp, null, edi, l };
                yield return new object[] { mfp, emp, null, l };
                yield return new object[] { mfp, emp, edi, null };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidCtorParameters))]
        public void ShouldThrowExceptionsForNull(IMailFileProcessor mfp, IEventMessagePublisher emp, IEdi835MessagePublisher edi, ILogger<MailService> l)
        {
            MailService.Delay = (delayInMs) => Task.Delay(1);
            Assert.Throws<ArgumentNullException>(() => new MailService(mfp, emp, edi, l));
        }

        [Fact]
        public async Task ShouldPublishMessagesWhenTransactionsAvailable()
        {
            var data = CreateData(_random.Next(1, 10));

            await SUT.DistributeMail(data, new CancellationToken());

            _mailFileProcessor.Verify(_ => _.Process(
                It.IsAny<DocDistMsg>(),
                true),
                Times.Once);

            _eventMessagePublisher.Verify(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()),
                Times.Once);

            _mailFileProcessor.VerifyNoOtherCalls();
            _eventMessagePublisher.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPublishEventsEvenIfCallThrowsError()
        {
            var data = CreateData(_random.Next(1, 10));

            _mailFileProcessor.Setup(m => m.Process(It.IsAny<DocDistMsg>(), true))
                .ThrowsAsync(new Exception());

            await Assert.ThrowsAsync<Exception>(() => SUT.DistributeMail(data, new CancellationToken()));

            _eventMessagePublisher.Verify(e => e.PublishEvents(
                It.IsAny<DocDistMsg>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldDoNothingIfNoTransactionIds()
        {
            var data = CreateData(0);

            await SUT.DistributeMail(data, new CancellationToken());

            _mailFileProcessor.VerifyNoOtherCalls();
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
