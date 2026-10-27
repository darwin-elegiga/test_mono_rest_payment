using Bogus;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.DocSys.Storage;
using VPay.EventBus.Abstractions;
using VPay.EventBus.Messages.DocSys;

using Xunit;

namespace VPay.Distribution.Tests
{
    public class EventDistributionQueueMessageProcessorTests
    {
        private readonly Mock<IStorageApiClient> _storageApiClient = new Mock<IStorageApiClient>();
        private readonly Mock<IEventBusPublisher> _eventBusPublisher = new Mock<IEventBusPublisher>();

        private readonly IEventDistributionQueueMessageProcessor SUT;

        private readonly Faker Faker = new Faker();

        public EventDistributionQueueMessageProcessorTests()
        {
            _eventBusPublisher
                .Setup(e => e.Publish(
                    It.IsAny<DistributionArchiveGenerationComplete>(),
                    typeof(DistributionArchiveGenerationComplete),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _eventBusPublisher
                .Setup(e => e.Publish(
                    It.IsAny<DocumentsAddedToTransactionAfterDistributionPacket>(),
                    typeof(DocumentsAddedToTransactionAfterDistributionPacket),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();


            SUT = new EventDistributionQueueMessageProcessor(
                _storageApiClient.Object,
                _eventBusPublisher.Object,
                new NullLogger<EventDistributionQueueMessageProcessor>());
        }

        [Theory]
        [MemberData(nameof(InvalidCtorValues))]
        public void ShouldThrowExceptionOnInvalidCtor(Type exception, IStorageApiClient storageApiClient, IEventBusPublisher eventBusPublisher, ILogger<EventDistributionQueueMessageProcessor> logger)
        {
            Assert.Throws(exception, () => new EventDistributionQueueMessageProcessor(storageApiClient, eventBusPublisher, logger));
        }

        public class WhenProcessMessage : EventDistributionQueueMessageProcessorTests
        {
            private List<ArchiveFile> Arrange(int genericDetailCount, int transactionDetailCount)
            {
                var genericArchive = CreateArchive(DocumentSystemType.Generic, genericDetailCount);
                var transactionArchive = CreateArchive(DocumentSystemType.Transaction, transactionDetailCount);

                var files = new List<ArchiveFile> { genericArchive, transactionArchive };

                _storageApiClient
                    .Setup(s => s.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(files)
                    .Verifiable();

                return files;
            }

            [Theory]
            [MemberData(nameof(TestDataNoArchives))]
            public async Task ShouldNotPublishAnyEventsWhenNoArchivesFound(EventPublishQueueMessage eventPublishRequest)
            {
                _storageApiClient
                    .Setup(s => s.GetArchivesForTransactionId(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Enumerable.Empty<ArchiveFile>())
                    .Verifiable();

                await SUT.ProcessMessage(eventPublishRequest, new CancellationToken());

                _eventBusPublisher.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TestData))]
            public async Task ShouldPublishOneMessagePerDocumentSystemTypeForAnyArchiveDetails(EventPublishQueueMessage eventPublishRequest, int genericDetailCount, int transactionDetailCount)
            {
                var file = Arrange(genericDetailCount, transactionDetailCount);

                await SUT.ProcessMessage(eventPublishRequest, new CancellationToken());

                _eventBusPublisher.Verify(e => e.Publish(
                        It.IsAny<DistributionArchiveGenerationComplete>(),
                        typeof(DistributionArchiveGenerationComplete),
                        It.IsAny<CancellationToken>()),
                    Times.Exactly(genericDetailCount == 0 ? 0 : 1));

                _eventBusPublisher.Verify(e => e.Publish(
                        It.IsAny<DocumentsAddedToTransactionAfterDistributionPacket>(),
                        typeof(DocumentsAddedToTransactionAfterDistributionPacket),
                        It.IsAny<CancellationToken>()),
                    Times.Exactly(transactionDetailCount == 0 ? 0 : 1));
            }

            [Theory]
            [MemberData(nameof(TestData))]
            public async Task ShouldPublishCorrectFileInformation(EventPublishQueueMessage eventPublishRequest, int genericDetailCount, int transactionDetailCount)
            {
                var archiveDetails = Arrange(genericDetailCount, transactionDetailCount)
                    .SelectMany(_ => _.ArchiveDetails)
                    .ToList();

                var f = new Func<List<FileInformation>, bool>(fi =>
                {
                    foreach (var f in fi)
                    {
                        var flag = archiveDetails.Any(a =>
                        a.ArchiveDetailId == f.ArchiveDetailId &&
                        a.Description == f.Description &&
                        a.FileName == f.FileName &&
                        a.FileDetailType.ToString()
                            .Equals(f.FileDetailType.ToString(), StringComparison.InvariantCultureIgnoreCase));

                        if (!flag) { return false; }
                    }

                    return true;
                });

                await SUT.ProcessMessage(eventPublishRequest, new CancellationToken());

                _eventBusPublisher.Verify(e => e.Publish(
                        It.Is<DistributionArchiveGenerationComplete>(_ => f(_.Files)),
                        typeof(DistributionArchiveGenerationComplete),
                        It.IsAny<CancellationToken>()),
                    Times.Exactly(genericDetailCount == 0 ? 0 : 1));

                _eventBusPublisher.Verify(e => e.Publish(
                        It.Is<DocumentsAddedToTransactionAfterDistributionPacket>(_ => f(_.Files)),
                        typeof(DocumentsAddedToTransactionAfterDistributionPacket),
                        It.IsAny<CancellationToken>()),
                    Times.Exactly(transactionDetailCount == 0 ? 0 : 1));
            }

            [Theory]
            [MemberData(nameof(InvalidParamData))]
            public async void ShouldThrowExceptionOnInvalidParameters(Type exception, EventPublishQueueMessage eventPublishRequest)
            {
                await Assert.ThrowsAsync(exception, () => SUT.ProcessMessage(eventPublishRequest, new CancellationToken()));
            }

            public static IEnumerable<object[]> TestData
            {
                get
                {
                    yield return new object[] { new EventPublishQueueMessage
                    {
                        ClientCode = "TESTTPA",
                        BillingEntityCode = "TESTBE",
                        OriginCode = "TESTORIGIN",
                        ClientPaymentId = "TESTID"
                    }, 0, 0 };

                    yield return new object[] { new EventPublishQueueMessage
                    {
                        ClientCode = "TESTTPA",
                        BillingEntityCode = "TESTBE",
                        OriginCode = "TESTORIGIN",
                        ClientPaymentId = "TESTID"
                    }, 0, 0 };

                    for (int genericDetailCount = 0; genericDetailCount <= 3; genericDetailCount++)
                    {
                        for (int transactionDetailCount = 0; transactionDetailCount <= 3; transactionDetailCount++)
                        {
                            yield return new object[] { new EventPublishQueueMessage
                            {
                                ClientCode = "TESTTPA",
                                BillingEntityCode = "TESTBE",
                                OriginCode = "TESTORIGIN",
                                ClientPaymentId = "TESTID",
                            }, genericDetailCount, transactionDetailCount };
                        }
                    }
                }
            }

            public static IEnumerable<object[]> TestDataNoArchives
            {
                get
                {
                    return TestData.Select(_ => new object[] { _.First() });
                }
            }

            public static IEnumerable<object[]> InvalidParamData
            {
                get
                {
                    yield return new object[] { typeof(ArgumentNullException), null };
                    yield return new object[] { typeof(ArgumentNullException), new EventPublishQueueMessage { } };
                    yield return new object[] { typeof(ArgumentException), new EventPublishQueueMessage { ClientCode = "" } };
                    yield return new object[] { typeof(ArgumentNullException), new EventPublishQueueMessage { ClientCode = "TESTTPA" } };
                    yield return new object[] { typeof(ArgumentException), new EventPublishQueueMessage { ClientCode = "TESTTPA", BillingEntityCode = "" } };
                    yield return new object[] { typeof(ArgumentNullException), new EventPublishQueueMessage { ClientCode = "TESTTPA", BillingEntityCode = "TESTBE" } };
                    yield return new object[] { typeof(ArgumentException), new EventPublishQueueMessage { ClientCode = "TESTTPA", BillingEntityCode = "TESTBE", OriginCode = "" } };
                    yield return new object[] { typeof(ArgumentNullException), new EventPublishQueueMessage { ClientCode = "TESTTPA", BillingEntityCode = "TESTBE", OriginCode = "TESTORIGIN" } };
                    yield return new object[] { typeof(ArgumentException), new EventPublishQueueMessage { ClientCode = "TESTTPA", BillingEntityCode = "TESTBE", OriginCode = "TESTORIGIN", ClientPaymentId = "" } };
                }
            }
        }

        public static IEnumerable<object[]> InvalidCtorValues
        {
            get
            {
                var storageApiClient = Mock.Of<IStorageApiClient>();
                var eventBusPublisher = Mock.Of<IEventBusPublisher>();
                var logger = new NullLogger<EventDistributionQueueMessageProcessor>();

                yield return new object[] { typeof(ArgumentNullException), null, eventBusPublisher, logger };
                yield return new object[] { typeof(ArgumentNullException), storageApiClient, null, logger };
                yield return new object[] { typeof(ArgumentNullException), storageApiClient, eventBusPublisher, null };
            }
        }

        private ArchiveFile CreateArchive(DocumentSystemType documentSystemType, int detailCount)
        {
            var details = new ArchiveDetail[detailCount];
            Array.Fill(details, CreateDetail());

            return new ArchiveFile
            {
                DocumentSystemType = documentSystemType,
                ArchiveDetails = details
            };
        }

        private ArchiveDetail CreateDetail()
        {
            return new ArchiveDetail
            {
                ArchiveDetailId = Faker.Random.Long(0),
                Description = Faker.Random.String2(10),
                FileName = Faker.Random.String(20),
                FileDetailType = Faker.Random.Enum<DocSys.Common.FileDetailType>()
            };
        }
    }
}
