using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;

using Xunit;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Queuing;
using Microsoft.Extensions.Logging;
using Bogus;
using FluentAssertions;

namespace VPay.Distribution.Tests
{
#pragma warning disable CA1034 // Nested types should not be visible
    public class EventMessagePublisherTests
    {
        private readonly Mock<IDistributionRepository> _distributionRepository = new Mock<IDistributionRepository>();
        private readonly Mock<IMessagePublisherService> _messagePublisher = new Mock<IMessagePublisherService>();

        private readonly IEventMessagePublisher SUT;

        private readonly static Faker Faker = new Faker();

        public EventMessagePublisherTests()
        {
            _distributionRepository
                .Setup(_ => _.GetPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<DocumentTransactionStatus>().ToList())
                .Verifiable();

            _messagePublisher
                .Setup(_ => _.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>()))
                .Verifiable();

            SUT = new EventMessagePublisher(
                _distributionRepository.Object,
                _messagePublisher.Object,
                new EventDistributionQueueConfigurationSettings(),
                new NullLogger<EventMessagePublisher>());
        }

        [Theory]
        [MemberData(nameof(InvalidCtorValues))]
        public void ShouldThrowExceptionOnInvalidCtor(Type e, IDistributionRepository dr, IMessagePublisherService mps, EventDistributionQueueConfigurationSettings qcs, ILogger<EventMessagePublisher> l)
        {
            Assert.Throws(e, () => new EventMessagePublisher(dr, mps, qcs, l));
        }

        public class WhenQueuingEventPublishMessages : EventMessagePublisherTests
        {
            [Theory]
            [MemberData(nameof(InvalidParamData))]
            public void ShouldThrowExceptionOnInvalidParameters(Type exception, DocDistMsg docDistMsg)
            {
                Assert.Throws(exception, () => SUT.PublishEvents(docDistMsg));
            }

            [Fact]
            public void ShouldDoNothingForNullTransactions()
            {
                var docDistMsg = CreateMessage(0, 0);

                docDistMsg.Transactions = null;

                SUT.PublishEvents(docDistMsg);

                _messagePublisher.VerifyNoOtherCalls();

                _distributionRepository.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TestMessages))]
            public void ShouldQueueMessagesForTransactions(int uniqueCount, int duplicateCount, int totalCount)
            {
                var docDistMsg = CreateMessage(uniqueCount, duplicateCount);

                SUT.PublishEvents(docDistMsg);

                foreach (var t in docDistMsg.Transactions)
                {
                    _messagePublisher
                        .Verify(_ => _.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.Is<string>(__ => __.Contains($"\"{nameof(EventPublishQueueMessage.TransactionId)}\":{t.TxnId}"))),
                        Times.Once);
                }

                _messagePublisher
                    .Verify(_ => _.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>()),
                    Times.Exactly(totalCount));

                _distributionRepository.VerifyNoOtherCalls();
            }

            public static IEnumerable<object[]> TestMessages
            {
                get
                {
                    for (int uniqueCount = 0; uniqueCount <= 3; uniqueCount++)
                    {
                        for (int duplicateCount = 2; duplicateCount <= 4; duplicateCount++)
                        {
                            yield return new object[]
                            {
                                uniqueCount,
                                duplicateCount,
                                uniqueCount + 1
                            };
                        }
                    }
                }
            }

            private DocDistMsg CreateMessage(int uniqueTransactions, int duplicateTransactions)
            {
                var ddm = new DocDistMsg
                {
                    TPA = "TESTTPA",
                    BillingEntity = "TESTBE",
                    Origin = "TESTORIGIN",
                    Transactions = new List<DistTxnData>()
                };

                for (int i = 0; i < uniqueTransactions; i++)
                {
                    ddm.Transactions.Add(new DistTxnData
                    {
                        TxnId = Faker.Random.Long(5),
                    });
                }

                var dupes = new DistTxnData[duplicateTransactions];
                Array.Fill(dupes, new DistTxnData { TxnId = Faker.Random.Long(5) });
                ddm.Transactions.AddRange(dupes);

                return ddm;
            }
        }

        public class WhenPublishEventsForTransactionIds : EventMessagePublisherTests
        {
            [Theory]
            [MemberData(nameof(InvalidParamData))]
            public async void ShouldThrowExceptionOnInvalidParameters(Type exception, DocDistMsg docDistMsg)
            {
                await Assert.ThrowsAsync(exception, () => SUT.ValidateAndPublishEventsForTransactionIds(docDistMsg, new CancellationToken()));
            }

            [Fact]
            public void ShouldDoNothingForNullTransactions()
            {
                var (docDistMsg, _) = CreateMessage(0, 0, 0);

                docDistMsg.Transactions = null;

                SUT.PublishEvents(docDistMsg);

                _messagePublisher.VerifyNoOtherCalls();

                _distributionRepository.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TestMessages))]
            public async Task ShouldPublishMessagesForTransactions(int readyCount, int notInDbCount, int noFinalDistCount)
            {
                var (docDistMsg, result) = CreateMessage(readyCount, notInDbCount, noFinalDistCount);

                _distributionRepository
                    .Setup(_ => _.GetPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(result)
                    .Verifiable();

                var actual = await SUT.ValidateAndPublishEventsForTransactionIds(docDistMsg, new CancellationToken());

                var txnList = docDistMsg.Transactions
                    .Select(_ => _.TxnId)
                    .ToList();
                var dbList = result
                    .Select(_ => _.TransactionId)
                    .ToList();

                using (new FluentAssertions.Execution.AssertionScope())
                {
                    actual.TransactionsWithEventsPublished.Count.Should().Be(readyCount);
                    actual.TransactionsNotDistributed.Count.Should().Be(noFinalDistCount);
                    actual.TransactionsWithoutDistributionStatus.Count.Should().Be(notInDbCount);

                    foreach (var t in actual.TransactionsWithEventsPublished)
                    {
                        Assert.Contains(t, txnList);
                        Assert.Contains(t, dbList);
                    }

                    foreach (var t in actual.TransactionsWithoutDistributionStatus)
                    {
                        Assert.Contains(t, txnList);
                        Assert.DoesNotContain(t, dbList);
                    }

                    foreach (var t in actual.TransactionsNotDistributed)
                    {
                        Assert.Contains(t, txnList);
                        Assert.Contains(t, dbList);
                    }
                }

                foreach (var t in docDistMsg.Transactions)
                {
                    var dbRecord = !(result.FirstOrDefault(_ => _.TransactionId == t.TxnId)?.FinalDistType is null);

                    _messagePublisher
                        .Verify(_ => _.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.Is<string>(__ => __.Contains($"\"{nameof(EventPublishQueueMessage.TransactionId)}\":{t.TxnId}"))),
                        dbRecord ? Times.Once() : Times.Never());
                }

                _messagePublisher
                    .Verify(_ => _.PublishMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>()),
                    Times.Exactly(readyCount));

                _distributionRepository
                    .Verify(_ => _.GetPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()),
                    (readyCount + notInDbCount + noFinalDistCount) == 0 ? Times.Never() : Times.Once());
            }

            public static IEnumerable<object[]> TestMessages
            {
                get
                {
                    for (int readyCount = 0; readyCount <= 3; readyCount++)
                    {
                        for (int notInDbCount = 0; notInDbCount <= 3; notInDbCount++)
                        {
                            for (int noFinalDistCount = 0; noFinalDistCount <= 3; noFinalDistCount++)
                            {
                                {
                                    yield return new object[] { readyCount, notInDbCount, noFinalDistCount };
                                }
                            }
                        }
                    }
                }
            }

            private (DocDistMsg, List<DocumentTransactionStatus>) CreateMessage(int readyCount, int notInDbCount, int noFinalDistCount)
            {
                var ddm = new DocDistMsg
                {
                    TPA = "TESTTPA",
                    BillingEntity = "TESTBE",
                    Origin = "TESTORIGIN",
                    Transactions = new List<DistTxnData>()
                };

                var ready = new List<DistTxnData>(readyCount);
                var noDb = new List<DistTxnData>(notInDbCount);
                var noFinal = new List<DistTxnData>(noFinalDistCount);

                var result = new List<DocumentTransactionStatus>();

                for (int i = 0; i < readyCount; i++)
                {
                    var txid = Faker.Random.Long(5);

                    ready.Add(new DistTxnData
                    {
                        TxnId = txid
                    });

                    result.Add(new DocumentTransactionStatus
                    {
                        TransactionId = txid,
                        FinalDistType = DistributionMethod.Fax,
                    });
                }

                for (int i = 0; i < notInDbCount; i++)
                {
                    var txid = Faker.Random.Long(5);

                    noDb.Add(new DistTxnData
                    {
                        TxnId = txid
                    });
                }

                for (int i = 0; i < noFinalDistCount; i++)
                {
                    var txid = Faker.Random.Long(5);

                    noFinal.Add(new DistTxnData
                    {
                        TxnId = txid,
                    });

                    result.Add(new DocumentTransactionStatus
                    {
                        TransactionId = txid,
                        FinalDistType = null,
                    });
                }

                ddm.Transactions = Array.Empty<DistTxnData>()
                    .Union(ready)
                    .Union(noDb)
                    .Union(noFinal)
                    .ToList();

                return (ddm, result);
            }
        }

        public static IEnumerable<object[]> InvalidCtorValues
        {
            get
            {
                var dr = Mock.Of<IDistributionRepository>();
                var mps = Mock.Of<IMessagePublisherService>();
                var qcs = new EventDistributionQueueConfigurationSettings();
                var l = new NullLogger<EventMessagePublisher>();

                yield return new object[] { typeof(ArgumentNullException), null, mps, qcs, l };
                yield return new object[] { typeof(ArgumentNullException), dr, null, qcs, l };
                yield return new object[] { typeof(ArgumentNullException), dr, mps, null, l };
                yield return new object[] { typeof(ArgumentNullException), dr, mps, qcs, null };
            }
        }

        public static IEnumerable<object[]> InvalidParamData
        {
            get
            {
                yield return new object[] { typeof(ArgumentNullException), null };
                yield return new object[] { typeof(ArgumentNullException), new DocDistMsg { } };
                yield return new object[] { typeof(ArgumentException), new DocDistMsg { TPA = "" } };
                yield return new object[] { typeof(ArgumentNullException), new DocDistMsg { TPA = "TESTTPA" } };
                yield return new object[] { typeof(ArgumentException), new DocDistMsg { TPA = "TESTTPA", BillingEntity = "" } };
                yield return new object[] { typeof(ArgumentNullException), new DocDistMsg { TPA = "TESTTPA", BillingEntity = "TESTBE" } };
                yield return new object[] { typeof(ArgumentException), new DocDistMsg { TPA = "TESTTPA", BillingEntity = "TESTBE", Origin = "" } };
            }
        }
    }
#pragma warning restore CA1034 // Nested types should not be visible
}
