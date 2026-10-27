using Bogus;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Queuing;

using Xunit;

namespace VPay.Distribution.Tests
{
#pragma warning disable CA1034 // Nested types should not be visible

    public class DistributionQueueMessageProcessorTests
    {
        protected readonly Mock<IDistributionRepository> _distributionRepository = new Mock<IDistributionRepository>();
        protected readonly Mock<IMessagePublisherService> _messagePublisherService = new Mock<IMessagePublisherService>();
        protected readonly DistributionQueueConfigurationSettings _distributionQueueConfigurationSettings;
        protected readonly FaxDistributionQueueConfigurationSettings _faxDistributionQueueConfigurationSettings;
        protected readonly MailDistributionQueueConfigurationSettings _mailDistributionQueueConfigurationSettings;
        protected readonly SendBackDistributionQueueConfigurationSettings _sendBackDistributionQueueConfigurationSettings;
        private readonly STPQueueConfigurationSettings _stpDistributionQueueConfigurationSettings;

        protected readonly DelayConfig _delayConfig;

        protected readonly IDistributionQueueMessageProcessor SUT;

        protected DistributionStatusRecord distributionStatusRecord = new DistributionStatusRecord();

        protected List<DocumentTransactionStatus> dbRecords = new List<DocumentTransactionStatus>();

        protected Faker Faker = new Faker();

        protected DocDistMsg message;

        public DistributionQueueMessageProcessorTests()
        {
            _distributionRepository
                .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            _distributionRepository
                .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            _distributionRepository
                .Setup(s => s.GetIsMailPaymentsComplete(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            _distributionRepository
                .Setup(s => s.GetIsStraightThruProcessingPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .Verifiable();

            _distributionRepository
                .Setup(s => s.GetDistributionStatus(It.IsAny<long>(), It.IsAny<DistributionMethod>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(distributionStatusRecord)
                .Verifiable();

            _distributionRepository
                .Setup(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<DistributionMethod>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbRecords)
                .Verifiable();

            _messagePublisherService
                .Setup(m => m.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>()))
                .Verifiable();

            _distributionQueueConfigurationSettings = new DistributionQueueConfigurationSettings();
            _faxDistributionQueueConfigurationSettings = new FaxDistributionQueueConfigurationSettings();
            _mailDistributionQueueConfigurationSettings = new MailDistributionQueueConfigurationSettings();
            _sendBackDistributionQueueConfigurationSettings = new SendBackDistributionQueueConfigurationSettings();
            _stpDistributionQueueConfigurationSettings = new STPQueueConfigurationSettings();

            _delayConfig = new DelayConfig
            {
                StatusCheckDelay = 1,
                WarningIssueDelay = 2
            };

            SUT = new DistributionQueueMessageProcessor(
                new NullLogger<DistributionQueueMessageProcessor>(),
                _distributionRepository.Object,
                _messagePublisherService.Object,
                _distributionQueueConfigurationSettings,
                _faxDistributionQueueConfigurationSettings,
                _mailDistributionQueueConfigurationSettings,
                _sendBackDistributionQueueConfigurationSettings,
                _stpDistributionQueueConfigurationSettings,
                _delayConfig);
        }

        protected void AddTransactions(int txnCount)
        {
            for (int i = 0; i < txnCount; i++)
            {
                message.Transactions.Add(new DistTxnData
                {
                    TxnId = Faker.Random.Long(1),
                    DistType = 'T'
                });
            }

            message.BatchSize = message.Transactions.Count;
        }

        public class ShouldThrowException : DistributionQueueMessageProcessorTests
        {
            public ShouldThrowException() : base()
            {
            }

            [Theory]
            [MemberData(nameof(InvalidCtorParameters))]
            public void ShouldThrowExceptionOnNullParams(Type exception, IDistributionRepository dr, IMessagePublisherService mps, DistributionQueueConfigurationSettings dqcs, FaxDistributionQueueConfigurationSettings fdqcs, MailDistributionQueueConfigurationSettings mdqcs, SendBackDistributionQueueConfigurationSettings sbdqcs, STPQueueConfigurationSettings stpdcs, DelayConfig dc, ILogger<DistributionQueueMessageProcessor> l)
            {
                Assert.Throws(exception, () => new DistributionQueueMessageProcessor(l, dr, mps, dqcs, fdqcs, mdqcs, sbdqcs, stpdcs, dc));
            }

            public static IEnumerable<object[]> InvalidCtorParameters
            {
                get
                {
                    var so = Mock.Of<IDistributionRepository>();
                    var mps = Mock.Of<IMessagePublisherService>();
                    var dqcs = new DistributionQueueConfigurationSettings();
                    var fdqcs = new FaxDistributionQueueConfigurationSettings();
                    var mdqcs = new MailDistributionQueueConfigurationSettings();
                    var sbdqcs = new SendBackDistributionQueueConfigurationSettings();
                    var stpdcs = new STPQueueConfigurationSettings();
                    var dc = new DelayConfig();
                    var l = new NullLogger<DistributionQueueMessageProcessor>();

                    yield return new object[] { typeof(ArgumentNullException), null, mps, dqcs, fdqcs, mdqcs, sbdqcs, stpdcs, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, null, dqcs, fdqcs, mdqcs, sbdqcs, stpdcs, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, mps, null, fdqcs, mdqcs, sbdqcs, stpdcs, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, mps, dqcs, null, mdqcs, sbdqcs, stpdcs, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, mps, dqcs, fdqcs, null, sbdqcs, stpdcs, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, mps, dqcs, fdqcs, mdqcs, null, stpdcs, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, mps, dqcs, fdqcs, mdqcs, sbdqcs, null, dc, l };
                    yield return new object[] { typeof(ArgumentNullException), so, mps, dqcs, fdqcs, mdqcs, sbdqcs, stpdcs, dc, null };
                }
            }
        }

        public class WhenNextStatusCheckHasNotPassed : DistributionQueueMessageProcessorTests
        {
            public WhenNextStatusCheckHasNotPassed() : base()
            {
            }

            [Fact]
            public async Task ShouldRequeue()
            {
                var futureTime = DateTime.Now.AddHours(1);

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);
                    return m.NextStatusCheck == futureTime;
                });

                message = new DocDistMsg
                {
                    NextStatusCheck = futureTime
                };

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenWarningTimeHasPassed : DistributionQueueMessageProcessorTests
        {
            public WhenWarningTimeHasPassed() : base()
            {
                message = new DocDistMsg
                {
                    WarningTime = DateTime.Now.AddMinutes(-1),
                    NextStatusCheck = DateTime.Today.AddDays(-1),
                };
            }

            public static IEnumerable<object[]> TestFlags
            {
                get
                {
                    yield return new object[] { true, true, false, true, true };
                    yield return new object[] { true, false, true, true, true };
                    yield return new object[] { true, false, true, false, true };
                    yield return new object[] { true, false, false, true, true };
                    yield return new object[] { true, false, false, false, true };
                    yield return new object[] { false, true, true, true, true };
                    yield return new object[] { false, true, true, false, true };
                    yield return new object[] { false, true, false, true, true };
                    yield return new object[] { false, true, false, false, true };
                    yield return new object[] { false, false, true, true, true };
                    yield return new object[] { false, false, true, false, true };
                    yield return new object[] { false, false, false, true, true };
                    yield return new object[] { false, false, false, false, false };
                }
            }

            public static IEnumerable<object[]> InvalidTestFlags
            {
                get
                {
                    yield return new object[] { true, true, true, true, true };
                    yield return new object[] { true, true, true, false, true };
                }
            }

            [Theory]
            [MemberData(nameof(InvalidTestFlags))]
            public async Task ShouldNotRequeueWithEverythingComplete(bool fax, bool nonFax, bool sendBack, bool isSendBack, bool isStp)
            {
                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.CustomSendBackComplete = sendBack;
                message.IsCustomSendBackTpa = isSendBack;
                message.StraightThruProcessingComplete = isStp;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TestFlags))]
            public async Task ShouldRequeueWithWarningAddBy1(bool fax, bool nonFax, bool sendBack, bool isSendBack, bool isStp)
            {
                var warnings = 5;

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.CustomSendBackComplete = sendBack;
                message.IsCustomSendBackTpa = isSendBack;
                message.StraightThruProcessingComplete = isStp;
                message.WarningsIssued = warnings;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.WarningTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.WarningIssueDelay));

                    return m.WarningsIssued == warnings + 1;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TestFlags))]
            public async Task ShouldRequeueWithNewDelay(bool fax, bool nonFax, bool sendBack, bool isSendBack, bool isStp)
            {
                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.CustomSendBackComplete = sendBack;
                message.IsCustomSendBackTpa = isSendBack;
                message.StraightThruProcessingComplete = isStp;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.WarningTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.WarningIssueDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenSendBacksAreNotComplete : DistributionQueueMessageProcessorTests
        {
            public WhenSendBacksAreNotComplete() : base()
            {
                message = new DocDistMsg
                {
                    WarningTime = DateTime.Now.AddDays(1),
                    CustomSendBackComplete = false,
                    IsCustomSendBackTpa = true,
                    Transactions = new List<DistTxnData>()
                };
            }

            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { true, true, true };
                    yield return new object[] { true, false, true };
                    yield return new object[] { false, true, true };
                    yield return new object[] { false, false, false };
                    yield return new object[] { false, true, false };
                }
            }

            public static IEnumerable<object[]> DbCheckTestCases
            {
                get
                {
                    var hasDbRecord = new[] { true, false };

                    return from db in hasDbRecord
                           from arr in TestCases
                           select arr
                           .Append(db)
                           .ToArray();
                }
            }

            public static IEnumerable<object[]> TransactionTestCases
            {
                get
                {
                    var transactionCount = new[] { 1, 5 };

                    return from tc in transactionCount
                           from arr in TestCases
                           select arr
                           .Append(tc)
                           .ToArray();
                }
            }

            public static IEnumerable<object[]> TransactionTestCasesWithDbRecords
            {
                get
                {
                    foreach (var tc in TransactionTestCases)
                    {
                        // last value is the transaction count
                        for (int i = 1; i <= (int)tc.Last(); i++)
                        {
                            yield return tc
                                .Append(i)
                                .ToArray();
                        }
                    }
                }
            }

            [Theory]
            [MemberData(nameof(TestCases))]
            public async Task ShouldRequeueIfCompleteInDb(bool fax, bool nonFax, bool stp)
            {
                _distributionRepository
                    .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    return m.CustomSendBackComplete;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(DbCheckTestCases))]
            public async Task ShouldCheckForIsDistTypeCompleteInDb(bool fax, bool nonFax, bool stp, bool isDbComplete)
            {
                _distributionRepository
                   .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(isDbComplete)
                   .Verifiable();

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsSendBackPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Theory]
            [MemberData(nameof(TestCases))]
            public async Task ShouldRequeueWithDelayIfNotCompleteInDb(bool fax, bool nonFax, bool stp)
            {
                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeTransactionsIfNoneInDb(bool fax, bool nonFax, bool stp, int txnCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                AddTransactions(txnCount);

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsSendBackPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCasesWithDbRecords))]
            public async Task ShouldDistributeTransactionsIfAnyCompleteInDb(bool fax, bool nonFax, bool stp, int txnCount, int dbRecordCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                AddTransactions(txnCount);

                foreach (var txn in Faker.PickRandom(message.Transactions, dbRecordCount))
                {
                    dbRecords.Add(new DocumentTransactionStatus
                    {
                        TransactionId = txn.TxnId,
                        FinalDistType = DistributionMethod.None,
                    });
                }

                var f1 = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);
                    m.FaxComplete.Should().Be(fax);
                    m.NonFaxComplete.Should().Be(nonFax);

                    m.BatchSize = dbRecords.Count;

                    m.Transactions
                        .Select(_ => _.TxnId)
                        .Should().BeEquivalentTo(dbRecords.Select(_ => _.TransactionId));

                    m.BatchSize.Should().Be(dbRecordCount);

                    return true;
                });

                var f2 = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.BatchSize = txnCount;

                    message.Transactions.Should().HaveCount(m.Transactions.Count);

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsSendBackPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _sendBackDistributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f1(_))),
                    Times.Once);

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f2(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeIfAllCompleteInDb(bool fax, bool nonFax, bool stp, int txnCount)
            {
                distributionStatusRecord.IsDistributionComplete = true;

                _distributionRepository
                    .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                AddTransactions(txnCount);

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsSendBackPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldThrowExceptionIfDbRecordNotInTransactions(bool fax, bool nonFax, bool stp, int txnCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsSendBackPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.FaxComplete = fax;
                message.NonFaxComplete = nonFax;
                message.StraightThruProcessingComplete = stp;

                AddTransactions(txnCount);

                dbRecords.Add(new DocumentTransactionStatus
                {
                    TransactionId = Faker.Random.Long(long.MinValue, 0),
                    FinalDistType = 'T'
                });

                await Assert.ThrowsAsync<Exception>(() => SUT.ProcessMessage(message, new CancellationToken()));

                _distributionRepository.Verify(s => s.GetIsSendBackPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.CustomSendBack,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenFaxIsNotComplete : DistributionQueueMessageProcessorTests
        {
            public WhenFaxIsNotComplete() : base()
            {
                message = new DocDistMsg
                {
                    WarningTime = DateTime.Now.AddDays(1),
                    FaxComplete = false,
                    CustomSendBackComplete = true,
                    IsCustomSendBackTpa = false,
                    Transactions = new List<DistTxnData>()
                };
            }

            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { true };
                    yield return new object[] { false };
                }
            }

            public static IEnumerable<object[]> DbCheckTestCases
            {
                get
                {
                    var hasDbRecord = new[] { true, false };

                    return from db in hasDbRecord
                           from arr in TestCases
                           select arr
                           .Append(db)
                           .ToArray();
                }
            }

            public static IEnumerable<object[]> TransactionTestCases
            {
                get
                {
                    var transactionCount = new[] { 1, 5 };

                    return from tc in transactionCount
                           from arr in TestCases
                           select arr
                           .Append(tc)
                           .ToArray();
                }
            }

            public static IEnumerable<object[]> TransactionTestCasesWithDbRecords
            {
                get
                {
                    foreach (var tc in TransactionTestCases)
                    {
                        // last value is the transaction count
                        for (int i = 1; i <= (int)tc.Last(); i++)
                        {
                            yield return tc
                                .Append(i)
                                .ToArray();
                        }
                    }
                }
            }

            [Theory]
            [MemberData(nameof(TestCases))]
            public async Task ShouldRequeueIfCompleteInDb(bool nonFax)
            {
                _distributionRepository
                    .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.NonFaxComplete = nonFax;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    return m.FaxComplete;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(DbCheckTestCases))]
            public async Task ShouldCheckForIsDistTypeCompleteInDb(bool nonFax, bool isDbComplete)
            {
                _distributionRepository
                   .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(isDbComplete)
                   .Verifiable();

                message.NonFaxComplete = nonFax;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsFaxPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Theory]
            [MemberData(nameof(TestCases))]
            public async Task ShouldRequeueWithDelayIfNotCompleteInDb(bool nonFax)
            {
                message.NonFaxComplete = nonFax;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeTransactionsIfNoneInDb(bool nonFax, int txnCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                message.NonFaxComplete = nonFax;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsFaxPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCasesWithDbRecords))]
            public async Task ShouldDistributeTransactionsIfAnyCompleteInDb(bool nonFax, int txnCount, int dbRecordCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                message.NonFaxComplete = nonFax;

                foreach (var txn in Faker.PickRandom(message.Transactions, dbRecordCount))
                {
                    dbRecords.Add(new DocumentTransactionStatus
                    {
                        TransactionId = txn.TxnId,
                        FinalDistType = DistributionMethod.None,
                    });
                }

                var f1 = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);
                    m.NonFaxComplete.Should().Be(nonFax);

                    m.BatchSize = dbRecords.Count;

                    m.Transactions
                        .Select(_ => _.TxnId)
                        .Should().BeEquivalentTo(dbRecords.Select(_ => _.TransactionId));

                    m.BatchSize.Should().Be(dbRecordCount);

                    return true;
                });

                var f2 = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.BatchSize = txnCount;

                    message.Transactions.Should().HaveCount(m.Transactions.Count);

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsFaxPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _faxDistributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f1(_))),
                    Times.Once);

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f2(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeIfAllCompleteInDb(bool nonFax, int txnCount)
            {
                distributionStatusRecord.IsDistributionComplete = true;

                _distributionRepository
                    .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                message.NonFaxComplete = nonFax;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsFaxPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldThrowExceptionIfDbRecordNotInTransactions(bool nonFax, int txnCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsFaxPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                message.NonFaxComplete = nonFax;

                dbRecords.Add(new DocumentTransactionStatus
                {
                    TransactionId = Faker.Random.Long(long.MinValue, 0),
                    FinalDistType = 'T'
                });

                await Assert.ThrowsAsync<Exception>(() => SUT.ProcessMessage(message, new CancellationToken()));

                _distributionRepository.Verify(s => s.GetIsFaxPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.Fax,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenNonFaxIsNotComplete : DistributionQueueMessageProcessorTests
        {
            public WhenNonFaxIsNotComplete() : base()
            {
                message = new DocDistMsg
                {
                    WarningTime = DateTime.Now.AddDays(1),
                    FaxComplete = true,
                    NonFaxComplete = false,
                    CustomSendBackComplete = true,
                    IsCustomSendBackTpa = false,
                    Transactions = new List<DistTxnData>()
                };
            }

            public static IEnumerable<object[]> TransactionTestCases
            {
                get
                {
                    yield return new object[] { 1 };
                    yield return new object[] { 5 };
                }
            }

            public static IEnumerable<object[]> TransactionTestCasesWithDbRecords
            {
                get
                {
                    foreach (var tc in TransactionTestCases)
                    {
                        // last value is the transaction count
                        for (int i = 1; i <= (int)tc.Last(); i++)
                        {
                            yield return tc
                                .Append(i)
                                .ToArray();
                        }
                    }
                }
            }

            [Fact]
            public async Task ShouldRequeueIfCompleteInDb()
            {
                _distributionRepository
                    .Setup(s => s.GetIsMailPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    return m.FaxComplete;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldCheckForIsDistTypeCompleteInDb(bool isDbComplete)
            {
                _distributionRepository
                   .Setup(s => s.GetIsMailPaymentsComplete(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(isDbComplete)
                   .Verifiable();

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsMailPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Fact]
            public async Task ShouldRequeueWithDelayIfNotCompleteInDb()
            {
                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeTransactionsIfNoneInDb(int txnCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsMailPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsMailPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCasesWithDbRecords))]
            public async Task ShouldDistributeTransactionsIfAnyCompleteInDb(int txnCount, int dbRecordCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsMailPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                foreach (var txn in Faker.PickRandom(message.Transactions, dbRecordCount))
                {
                    dbRecords.Add(new DocumentTransactionStatus
                    {
                        TransactionId = txn.TxnId,
                        FinalDistType = DistributionMethod.None,
                    });
                }

                var f1 = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);
                    m.FaxComplete.Should().BeTrue();
                    m.NonFaxComplete.Should().BeFalse();

                    m.BatchSize = dbRecords.Count;

                    m.Transactions
                        .Select(_ => _.TxnId)
                        .Should().BeEquivalentTo(dbRecords.Select(_ => _.TransactionId));

                    m.BatchSize.Should().Be(dbRecordCount);

                    return true;
                });

                var f2 = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.BatchSize = txnCount;

                    message.Transactions.Should().HaveCount(m.Transactions.Count);

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsMailPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _mailDistributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f1(_))),
                    Times.Once);

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f2(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeIfAllCompleteInDb(int txnCount)
            {
                distributionStatusRecord.IsDistributionComplete = true;

                _distributionRepository
                    .Setup(s => s.GetIsMailPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsMailPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldThrowExceptionIfDbRecordNotInTransactions(int txnCount)
            {
                _distributionRepository
                    .Setup(s => s.GetIsMailPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                dbRecords.Add(new DocumentTransactionStatus
                {
                    TransactionId = Faker.Random.Long(long.MinValue, 0),
                    FinalDistType = 'T'
                });

                await Assert.ThrowsAsync<Exception>(() => SUT.ProcessMessage(message, new CancellationToken()));

                _distributionRepository.Verify(s => s.GetIsMailPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.Mail,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenStraightThruProcessingIsNotComplete : DistributionQueueMessageProcessorTests
        {
            public WhenStraightThruProcessingIsNotComplete() : base()
            {
                message = new DocDistMsg
                {
                    WarningTime = DateTime.Now.AddDays(1),
                    StraightThruProcessingComplete = false,
                    CustomSendBackComplete = true,
                    IsCustomSendBackTpa = false,
                    Transactions = new List<DistTxnData>()
                };
            }

            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { true };
                    yield return new object[] { false };
                }
            }

            public static IEnumerable<object[]> DbCheckTestCases
            {
                get
                {
                    var hasDbRecord = new[] { true, false };

                    return from db in hasDbRecord
                           from arr in TestCases
                           select arr
                           .Append(db)
                           .ToArray();
                }
            }

            public static IEnumerable<object[]> TransactionTestCases
            {
                get
                {
                    yield return new object[] { 1 };
                    yield return new object[] { 5 };
                }
            }

            public static IEnumerable<object[]> TransactionTestCasesWithDbRecords
            {
                get
                {
                    foreach (var tc in TransactionTestCases)
                    {
                        // last value is the transaction count
                        for (int i = 1; i <= (int)tc.Last(); i++)
                        {
                            yield return tc
                                .Append(i)
                                .ToArray();
                        }
                    }
                }
            }

            [Fact]
            public async Task ShouldRequeueIfCompleteInDb()
            {
                _distributionRepository
                    .Setup(s => s.GetIsStraightThruProcessingPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                message.FaxComplete = true;
                message.NonFaxComplete = true;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    return m.StraightThruProcessingComplete;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldCheckForIsDistTypeCompleteInDb(bool isDbComplete)
            {
                _distributionRepository
                   .Setup(s => s.GetIsStraightThruProcessingPaymentsComplete(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(isDbComplete)
                   .Verifiable();

                message.FaxComplete = true;
                message.NonFaxComplete = true;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsStraightThruProcessingPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Fact]
            public async Task ShouldRequeueWithDelayIfNotCompleteInDb()
            {
                message.FaxComplete = true;
                message.NonFaxComplete = true;

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionTestCases))]
            public async Task ShouldNotDistributeTransactionsIfNoneInDb(int txnCount)
            {
                message.FaxComplete = true;
                message.NonFaxComplete = true;

                _distributionRepository
                    .Setup(s => s.GetIsStraightThruProcessingPaymentsComplete(
                        It.IsAny<long>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();

                AddTransactions(txnCount);

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(s => s.GetIsStraightThruProcessingPaymentsComplete(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethods.StraightThruProcessing,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.Verify(s => s.GetPaymentsByDistributionMethod(
                    It.IsAny<long>(),
                    It.IsAny<bool>(),
                    DistributionMethods.StraightThruProcessing,
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }
    }

#pragma warning restore CA1034 // Nested types should not be visible
}
