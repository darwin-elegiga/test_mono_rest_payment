using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Distribution.Common;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using Xunit;

namespace VPay.Distribution.Tests
{
    public class STPQueueMessageProcessorTests
    {
        private readonly Mock<ISTPMessageQueueProcessor> _stpMessageQueueProcessor = new Mock<ISTPMessageQueueProcessor>();
        private readonly Mock<IStpPaymentService> _stpPaymentService = new Mock<IStpPaymentService>();
        private readonly ILogger<STPDistributionMessageQueueProcessor> _logger = new NullLogger<STPDistributionMessageQueueProcessor>();
        protected readonly ISTPMessageQueueProcessor SUT;
        protected DocDistMsg message;
        private readonly Random _random = new Random();

        public STPQueueMessageProcessorTests()
        {
            SUT = new STPDistributionMessageQueueProcessor(_stpPaymentService.Object, _logger);

            _stpMessageQueueProcessor
                .Setup(_ => _.ProcessMessage(It.IsAny<DocDistMsg>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DistributionProcessResult("STP"))
                .Verifiable();

        }

        public static IEnumerable<object[]> InvalidCtorParameters
        {
            get
            {
                var stp = Mock.Of<IStpPaymentService>();
                var log = new NullLogger<STPDistributionMessageQueueProcessor>();

                yield return new object[] { null, null };
                yield return new object[] { null, log  };
                yield return new object[] { stp,  null };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidCtorParameters))]
        public void ShouldThrowExceptionsForNull(
            IStpPaymentService stp,
            ILogger<STPDistributionMessageQueueProcessor> log)
        {
            Assert.Throws<ArgumentNullException>(() => new STPDistributionMessageQueueProcessor(stp, log));
        }

        [Fact]
        public async Task ShouldNotPublishPMBMessage_IfNoTransactionId()
        {
            var data = CreateData(true, 0);
            await SUT.ProcessMessage(data);
            _stpMessageQueueProcessor.VerifyNoOtherCalls();
            _stpPaymentService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPublishPMBMessage_IfTransactionIdsArePresent()
        {
            var data = CreateData(true, 1);
            await SUT.ProcessMessage(data);
            _stpPaymentService.Verify(e => e.PublishPmbMessage(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPublishPMBMessage_IfStpPaymentIsC2B()
        {
            var data = CreateData(true, 1);
            var newStpPayment = new StpPayment
            {
                TransactionId = 1,
                NetworkName = "MYNETWORK",
                EnrollType = EnrollTypes.C2B,
                Edi835Type = Edi835Types.SDS,
            };
            var validStpPayment = new StpPayment
            {
                TransactionId = 1,
                NetworkName = "MYNETWORK",
                EnrollType = EnrollTypes.C2B,
                Edi835Type = Edi835Types.SDS,
                RemitValid = true,
            };

            _stpPaymentService
                .Setup(_ => _.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newStpPayment)
                .Verifiable();

            _stpPaymentService
                .Setup(_ => _.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validStpPayment)
                .Verifiable();

            await SUT.ProcessMessage(data);

            _stpPaymentService.Verify(_ => _.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
            _stpPaymentService.Verify(_ => _.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
            _stpPaymentService.Verify(e => e.PublishPmbMessage(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ShouldNotPublishPMBMessage_IfStpPaymentIsB2B()
        {
            var data = CreateData(true, 1);
            var newStpPayment = new StpPayment
            {
                TransactionId = 1,
                NetworkName = "MYNETWORK",
                EnrollType = EnrollTypes.B2B,
                Edi835Type = Edi835Types.SDS,
            };
            var validStpPayment = new StpPayment
            {
                TransactionId = 1,
                NetworkName = "MYNETWORK",
                EnrollType = EnrollTypes.B2B,
                Edi835Type = Edi835Types.SDS,
                RemitValid = true,
            };

            _stpPaymentService
                .Setup(_ => _.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newStpPayment)
                .Verifiable();

            _stpPaymentService
                .Setup(_ => _.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validStpPayment)
                .Verifiable();

            await SUT.ProcessMessage(data);

            _stpPaymentService.Verify(_ => _.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
            _stpPaymentService.Verify(_ => _.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
            _stpPaymentService.Verify(e => e.PublishPmbMessage(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private DocDistMsg CreateData(bool hasCorrespondenceId, int transactionCount = 1)
        {
            var data = new DocDistMsg
            {
                BatchSize = transactionCount,
                BillingEntity = "TESTBE",                
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
