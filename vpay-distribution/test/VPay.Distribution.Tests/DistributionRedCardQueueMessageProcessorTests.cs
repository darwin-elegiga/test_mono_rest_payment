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
    public class DistributionRedCardQueueMessageProcessorTests
    {
        protected readonly Mock<IRedCardRepository> _redCardRepository = new Mock<IRedCardRepository>();
        protected readonly Mock<IDistributionRepository> _distributionRepository = new Mock<IDistributionRepository>();
        protected readonly Mock<IAuxiliaryFileProcessor> _auxiliaryFileProcessor = new Mock<IAuxiliaryFileProcessor>();
        protected readonly Mock<IMessagePublisherService> _messagePublisherService = new Mock<IMessagePublisherService>();
        protected readonly RedCardDistributionQueueConfigurationSettings _distributionQueueConfigurationSettings;
        protected readonly FaxDistributionQueueConfigurationSettings _faxDistributionQueueConfigurationSettings;
        protected readonly DelayConfig _delayConfig;

        protected readonly IDistributionRedCardQueueMessageProcessor SUT;

        protected static Faker Faker = new Faker();

        protected DocDistMsg message;

        protected List<TransactionStatusRecord> transactionStatusRecords = new List<TransactionStatusRecord>();

        public DistributionRedCardQueueMessageProcessorTests()
        {
            _redCardRepository
                .Setup(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(RedCardFileStatusCode.NotFound)
                .Verifiable();

            _redCardRepository
                .Setup(r => r.GetCurrentTransactionStatusForStatusFileId(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionStatusRecords)
                .Verifiable();

            _redCardRepository
                .Setup(r => r.UpdateTransactionStatus(
                    It.IsAny<long>(),
                    RedCardTransactionStatusCode.TransactionComplete,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            _distributionRepository
                .Setup(r => r.TryUpdateDistributionStatus(
                    It.IsAny<long>(),
                    DistributionMethod.Fax,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .Verifiable();

            _auxiliaryFileProcessor.Setup(a => a.Process(
                It.IsAny<AuxiliaryMessage>()))
                .ReturnsAsync(new RedCardProcessResult
                {
                    ControlFileNames = new List<string> { "control-test" },
                    FileNames = new List<string> { "file-test" }
                })
                .Verifiable();

            _messagePublisherService
               .Setup(m => m.PublishMessage(
                   It.IsAny<QueueConfigurationSettings>(),
                   It.IsAny<string>()))
               .Verifiable();

            _distributionQueueConfigurationSettings = new RedCardDistributionQueueConfigurationSettings();
            _faxDistributionQueueConfigurationSettings = new FaxDistributionQueueConfigurationSettings();

            _delayConfig = new DelayConfig
            {
                StatusCheckDelay = 1,
                WarningIssueDelay = 2
            };

            DistributionRedCardQueueMessageProcessor.Delay = (delayInMs) => Task.Delay(1);
            SUT = new DistributionRedCardQueueMessageProcessor(
                new NullLogger<DistributionRedCardQueueMessageProcessor>(),
                _redCardRepository.Object,
                _distributionRepository.Object,
                _auxiliaryFileProcessor.Object,
                _messagePublisherService.Object,
                _distributionQueueConfigurationSettings,
                _faxDistributionQueueConfigurationSettings,
                _delayConfig);
        }

        public class ShouldThrowException : DistributionRedCardQueueMessageProcessorTests
        {
            public ShouldThrowException() : base() { }

            [Theory]
            [MemberData(nameof(InvalidCtorParameters))]
            public void ShouldThrowExpectionOnNullParams(
                Type exception,
                ILogger<DistributionRedCardQueueMessageProcessor> logger,
                IRedCardRepository redCardRepository,
                IDistributionRepository distributionRepository,
                IAuxiliaryFileProcessor auxiliaryFileProcessor,
                IMessagePublisherService messagePublisherService,
                RedCardDistributionQueueConfigurationSettings redCardDistributionQueueConfigurationSettings,
                FaxDistributionQueueConfigurationSettings faxDistributionQueueConfigurationSettings,
                DelayConfig dc)
            {
                DistributionRedCardQueueMessageProcessor.Delay = (delayInMs) => Task.Delay(1);

                Assert.Throws(exception, () =>
                {
                    return new DistributionRedCardQueueMessageProcessor(
                        logger,
                        redCardRepository,
                        distributionRepository,
                        auxiliaryFileProcessor,
                        messagePublisherService,
                        redCardDistributionQueueConfigurationSettings,
                        faxDistributionQueueConfigurationSettings,
                        dc);
                });
            }

            public static IEnumerable<object[]> InvalidCtorParameters
            {
                get
                {
                    var l = new NullLogger<DistributionRedCardQueueMessageProcessor>();
                    var rcs = Mock.Of<IRedCardRepository>();
                    var so = Mock.Of<IDistributionRepository>();
                    var afp = Mock.Of<IAuxiliaryFileProcessor>();
                    var mps = Mock.Of<IMessagePublisherService>();
                    var dqcs = new RedCardDistributionQueueConfigurationSettings();
                    var fdqcs = new FaxDistributionQueueConfigurationSettings();
                    var dc = new DelayConfig();

                    yield return new object[] { typeof(ArgumentNullException), null, rcs, so, afp, mps, dqcs, fdqcs, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, null, so, afp, mps, dqcs, fdqcs, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, rcs, null, afp, mps, dqcs, fdqcs, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, rcs, so, null, mps, dqcs, fdqcs, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, rcs, so, afp, null, dqcs, fdqcs, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, rcs, so, afp, mps, null, fdqcs, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, rcs, so, afp, mps, dqcs, null, dc };
                    yield return new object[] { typeof(ArgumentNullException), l, rcs, so, afp, mps, dqcs, fdqcs, null };
                }
            }

            [Fact]
            public async Task WhenStatusFileNotFound()
            {
                message = new DocDistMsg
                {
                    Origin = "RC",
                    WarningTime = DateTime.Now.AddDays(1),
                    NextStatusCheck = DateTime.Now.AddDays(-1),
                };

                await Assert.ThrowsAsync<Exception>(() => SUT.ProcessMessage(message, new CancellationToken()));

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()), Times.Once);

                _redCardRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task WhenMessageIsNotForARedCardTpa()
            {
                message = new DocDistMsg
                {
                    Origin = "IC",
                    WarningTime = DateTime.Now.AddDays(1),
                    NextStatusCheck = DateTime.Now.AddDays(-1),
                };

                await Assert.ThrowsAsync<Exception>(() => SUT.ProcessMessage(message, new CancellationToken()));

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenNextStatusCheckHasNotPassed : DistributionRedCardQueueMessageProcessorTests
        {
            public WhenNextStatusCheckHasNotPassed() : base()
            {
                message = new DocDistMsg
                {
                    Origin = "RC",
                    WarningTime = DateTime.Now.AddDays(1),
                };
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

                message.NextStatusCheck = futureTime;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenOnlyStatusFileIsParsed : DistributionRedCardQueueMessageProcessorTests
        {
            public WhenOnlyStatusFileIsParsed() : base()
            {
                message = new DocDistMsg
                {
                    Origin = "RC",
                    WarningTime = DateTime.Now.AddDays(1),
                    NextStatusCheck = DateTime.Now.AddDays(-1),
                };

                _redCardRepository
               .Setup(r => r.GetFileStatus(
                   It.IsAny<long>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(RedCardFileStatusCode.StatusFileParsed)
               .Verifiable();
            }

            [Fact]
            public async Task ShouldRequeueWithNewDelay()
            {
                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenOnlyAuxiliaryFileGenerated : DistributionRedCardQueueMessageProcessorTests
        {
            public WhenOnlyAuxiliaryFileGenerated() : base()
            {
                message = new DocDistMsg
                {
                    Origin = "RC",
                    WarningTime = DateTime.Now.AddDays(1),
                    NextStatusCheck = DateTime.Now.AddDays(-1),
                };

                _redCardRepository
               .Setup(r => r.GetFileStatus(
                   It.IsAny<long>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(RedCardFileStatusCode.AuxiliaryFileGenerated)
               .Verifiable();
            }

            public static IEnumerable<object[]> InvalidTransactionStatusCodes
            {
                get
                {
                    return Enum.GetValues(typeof(RedCardTransactionStatusCode))
                        .Cast<RedCardTransactionStatusCode>()
                        .Where(r => r < RedCardTransactionStatusCode.DocumentsGenerated)
                        .Select(r => new object[] { r });
                }
            }

            public static IEnumerable<object[]> TransactionStatusCodes
            {
                get
                {
                    return Enum.GetValues(typeof(RedCardTransactionStatusCode))
                        .Cast<RedCardTransactionStatusCode>()
                        .Where(r => r >= RedCardTransactionStatusCode.DocumentsGenerated)
                        .Select(r => new object[] { r });
                }
            }

            [Theory]
            [MemberData(nameof(InvalidTransactionStatusCodes))]
            public async Task ShouldRequeueWithNewDelay(RedCardTransactionStatusCode redCardTransactionStatusCode)
            {
                transactionStatusRecords.Add(new TransactionStatusRecord
                {
                    StatusCode = redCardTransactionStatusCode
                });

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.Verify(r => r.GetCurrentTransactionStatusForStatusFileId(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionStatusCodes))]
            public async Task ShouldRequeueMessage(RedCardTransactionStatusCode redCardTransactionStatusCode)
            {
                transactionStatusRecords.Add(new TransactionStatusRecord
                {
                    StatusCode = redCardTransactionStatusCode
                });

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    return m.NextStatusCheck == message.NextStatusCheck
                    && m.WarningTime == message.WarningTime;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.Verify(r => r.GetCurrentTransactionStatusForStatusFileId(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(TransactionStatusCodes))]
            public async Task ShouldTransferAuxiliaryFile(RedCardTransactionStatusCode redCardTransactionStatusCode)
            {
                transactionStatusRecords.Add(new TransactionStatusRecord
                {
                    StatusCode = redCardTransactionStatusCode
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.Verify(a => a.Process(
                    It.IsAny<AuxiliaryMessage>()),
                    Times.Once);

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenAuxiliaryFileTransferred : DistributionRedCardQueueMessageProcessorTests
        {
            public WhenAuxiliaryFileTransferred() : base()
            {
                message = new DocDistMsg
                {
                    Origin = "RC",
                    WarningTime = DateTime.Now.AddDays(1),
                    NextStatusCheck = DateTime.Now.AddDays(-1),
                };

                _redCardRepository
               .Setup(r => r.GetFileStatus(
                   It.IsAny<long>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(RedCardFileStatusCode.AuxiliaryFileTransferred)
               .Verifiable();
            }

            public static IEnumerable<object[]> InvalidTransactionStatusCodes
            {
                get
                {
                    return Enum.GetValues(typeof(RedCardTransactionStatusCode))
                        .Cast<RedCardTransactionStatusCode>()
                        .Where(r => r < RedCardTransactionStatusCode.DocumentsGenerated)
                        .Select(r => new object[] { r });
                }
            }

            public static IEnumerable<object[]> TransactionStatusCodes
            {
                get
                {
                    return Enum.GetValues(typeof(RedCardTransactionStatusCode))
                        .Cast<RedCardTransactionStatusCode>()
                        .Where(r => r >= RedCardTransactionStatusCode.DocumentsGenerated)
                        .Select(r => new object[] { r });
                }
            }

            [Theory]
            [MemberData(nameof(InvalidTransactionStatusCodes))]
            public async Task ShouldRequeueWithNewDelay(RedCardTransactionStatusCode redCardTransactionStatusCode)
            {
                transactionStatusRecords.Add(new TransactionStatusRecord
                {
                    StatusCode = redCardTransactionStatusCode
                });

                var f = new Func<string, bool>(s =>
                {
                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(_delayConfig.StatusCheckDelay));

                    return true;
                });

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.Verify(r => r.GetCurrentTransactionStatusForStatusFileId(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.VerifyNoOtherCalls();

                _auxiliaryFileProcessor.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => f(_))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }

        public class WhenProcessingTransactions : DistributionRedCardQueueMessageProcessorTests
        {
            public WhenProcessingTransactions() : base()
            {
                message = new DocDistMsg
                {
                    Origin = "RC",
                    WarningTime = DateTime.Now.AddDays(1),
                    NextStatusCheck = DateTime.Now.AddDays(-1),
                    Transactions = new List<DistTxnData>()
                };

                _redCardRepository
               .Setup(r => r.GetFileStatus(
                   It.IsAny<long>(),
                   It.IsAny<CancellationToken>()))
               .ReturnsAsync(RedCardFileStatusCode.AuxiliaryFileTransferred)
               .Verifiable();
            }

            private void AddTransactionAndRecord(string distType, RedCardTransactionStatusCode redCardTransactionStatusCode)
            {
                var txid = Faker.Random.Long(0);

                message.Transactions.Add(new DistTxnData
                {
                    TxnId = txid,
                    DistType = distType.First()
                });

                transactionStatusRecords.Add(new TransactionStatusRecord
                {
                    TransactionId = txid,
                    StatusCode = redCardTransactionStatusCode
                });
            }

            public static string[] NonFaxDistTypes = new[] { DistType.NoFaxNum, DistType.Mail, DistType.DisplayOnly, DistType.SendBack, " " };

            public static string[] FaxDistTypes = new[] { DistType.Fax, "R" };

            public static RedCardTransactionStatusCode[] RedCardTransactionStatusCodes = Enum.GetValues(typeof(RedCardTransactionStatusCode))
                .Cast<RedCardTransactionStatusCode>()
                .ToArray();

            public static IEnumerable<object[]> FaxParameters
            {
                get
                {
                    return FaxDistTypes.Select(r => new object[] { r });
                }
            }

            public static IEnumerable<object[]> NonFaxAndStatusAtLeastDocumentsGeneratedParameters
            {
                get
                {
                    return from dt in NonFaxDistTypes
                           from tc in RedCardTransactionStatusCodes
                           .Where(r => r >= RedCardTransactionStatusCode.DocumentsGenerated)
                           select new object[] { tc, dt };
                }
            }

            public static IEnumerable<object[]> FaxAndStatusAtLeastDocumentsGeneratedButNotCompletedTransactionParameters
            {
                get
                {
                    foreach (string dt in FaxDistTypes)
                    {
                        foreach (RedCardTransactionStatusCode tc in RedCardTransactionStatusCodes
                            .Where(r => r >= RedCardTransactionStatusCode.DocumentsGenerated
                            && !new[] { RedCardTransactionStatusCode.ReadyForDistribution, RedCardTransactionStatusCode.TransactionComplete }.Contains(r)))
                        {
                            var txid = Faker.Random.Long(0);

                            yield return new object[] {
                                new DistTxnData
                                {
                                    DistType = dt.First(),
                                    TxnId = txid
                                },
                                new TransactionStatusRecord
                                {
                                    StatusCode = tc,
                                    TransactionId = txid
                                },
                                new Func<string,int, bool>((s,i) =>
                                {
                                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);
                                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(i));

                                    return true;
                                })
                            };
                        }
                    }
                }
            }

            public static IEnumerable<object[]> FaxAndStatusAtLeastDocumentsGeneratedButDifferentTxIdTransactionParameters
            {
                get
                {
                    foreach (string dt in FaxDistTypes)
                    {
                        foreach (RedCardTransactionStatusCode tc in RedCardTransactionStatusCodes
                            .Where(r => r >= RedCardTransactionStatusCode.DocumentsGenerated))
                        {
                            var txid1 = Faker.Random.Long(0, 1000);
                            var txid2 = Faker.Random.Long(1001);

                            yield return new object[] {
                                new DistTxnData
                                {
                                    DistType = dt.First(),
                                    TxnId = txid1
                                },
                                new TransactionStatusRecord
                                {
                                    StatusCode = tc,
                                    TransactionId = txid2
                                },
                                new Func<string,int, bool>((s,i) =>
                                {
                                    var m = JsonConvert.DeserializeObject<DocDistMsg>(s);

                                    m.NextStatusCheck.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(i));
                                    m.Transactions.Select(t => t.TxnId).Should().Contain(txid1);
                                    m.Transactions.Select(t => t.TxnId).Should().NotContain(txid2);

                                    return true;
                                })
                            };
                        }
                    }
                }
            }

            [Theory]
            [MemberData(nameof(FaxParameters))]
            public async Task ShouldDistribute(string distType)
            {
                AddTransactionAndRecord(distType, RedCardTransactionStatusCode.ReadyForDistribution);

                var txCount = message.Transactions.Count;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.Verify(r => r.TryUpdateDistributionStatus(
                   It.IsAny<long>(),
                   DistributionMethod.Fax,
                   It.IsAny<CancellationToken>()),
                   Times.Once);

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.Verify(r => r.GetCurrentTransactionStatusForStatusFileId(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Exactly(2));

                _redCardRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _faxDistributionQueueConfigurationSettings)),
                    It.IsAny<string>()),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }

            [Theory]
            [MemberData(nameof(FaxAndStatusAtLeastDocumentsGeneratedButNotCompletedTransactionParameters))]
            [MemberData(nameof(FaxAndStatusAtLeastDocumentsGeneratedButDifferentTxIdTransactionParameters))]
            public async Task ShouldRequeueWithDelayTransaction(DistTxnData distTxnData, TransactionStatusRecord transactionRecord, Func<string, int, bool> validation)
            {
                message.Transactions.Add(distTxnData);

                transactionStatusRecords.Add(transactionRecord);

                var txCount = message.Transactions.Count;

                var result = await SUT.ProcessMessage(message, new CancellationToken());

                result.Should().BeTrue();

                _distributionRepository.VerifyNoOtherCalls();

                _redCardRepository.Verify(r => r.GetFileStatus(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

                _redCardRepository.Verify(r => r.GetCurrentTransactionStatusForStatusFileId(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                    Times.Exactly(2));

                _redCardRepository.VerifyNoOtherCalls();

                _messagePublisherService.Verify(m => m.PublishMessage(
                    It.Is<QueueConfigurationSettings>(_ => object.ReferenceEquals(_, _distributionQueueConfigurationSettings)),
                    It.Is<string>(_ => validation(_, _delayConfig.StatusCheckDelay))),
                    Times.Once);

                _messagePublisherService.VerifyNoOtherCalls();
            }
        }
    }
#pragma warning restore CA1034 // Nested types should not be visible
}
