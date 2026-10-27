using Microsoft.Extensions.Logging;

using Moq;

using System;
using System.Collections.Generic;
using System.Linq;

using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class TiffMessageQueuerTests
    {
        private readonly byte maxPriority = 10;
        private readonly Mock<IQueueService> _queueService = new Mock<IQueueService>();
        private readonly Mock<ILogger<TiffMessageQueuer>> logger = new Mock<ILogger<TiffMessageQueuer>>();

        private readonly ITiffMessageQueuer SUT;

        public TiffMessageQueuerTests()
        {
            var queueConfigurations = new QueueConfigurations
            {
                TiffQueue = new QueueConfigurationSettings
                {
                    QueueName = "TEST",
                    ErrorQueueName = "TEST-ERR",
                    MaxPriority = maxPriority,
                }
            };

            _queueService.Setup(_ => _.PublishQueueMessage(
                It.IsAny<QueueConfigurationSettings>(),
                It.IsAny<string>(),
                It.IsAny<byte?>()));

            SUT = new TiffMessageQueuer(queueConfigurations,
                _queueService.Object,
                logger.Object);
        }

        [Fact]
        public void ShouldThrowExceptionOnInvalidParamsInCtor()
        {
            var queueConfigurations = new QueueConfigurations
            {
                TiffQueue = new QueueConfigurationSettings
                {
                    MaxPriority = 1
                }
            };

            Assert.Throws<NullReferenceException>(() => new TiffMessageQueuer(
                null,
                _queueService.Object,
                logger.Object));

            Assert.Throws<ArgumentNullException>(() => new TiffMessageQueuer(
                queueConfigurations,
                null,
                logger.Object));

            Assert.Throws<ArgumentNullException>(() => new TiffMessageQueuer(
                queueConfigurations,
                _queueService.Object,
                null));

            queueConfigurations.TiffQueue.MaxPriority = 0;
            Assert.Throws<ArgumentException>(() => new TiffMessageQueuer(
                queueConfigurations,
                _queueService.Object,
                logger.Object));

            queueConfigurations.TiffQueue = null;
            Assert.Throws<ArgumentException>(() => new TiffMessageQueuer(
                queueConfigurations,
                _queueService.Object,
                logger.Object));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority">value of priority in the fax system,
        /// larger = lower priority
        /// (1 = highest, 100 = lowest)</param>
        /// <param name="scaledPriority">value or priority in the rabbit queue,
        /// smaller = lower priority
        /// (1 = lowest, 10 = highest)</param>
        [Theory]
        [InlineData(1, 10)]
        [InlineData(10, 10)]
        [InlineData(40, 7)]
        [InlineData(50, 6)]
        [InlineData(70, 4)]
        [InlineData(100, 1)]
        public void ShouldMapTiffQueueMessagePriorityToCorrectRMQPriorityValue(byte priority, byte scaledPriority)
        {
            var actual = new TiffQueueMessage
            {
                Priority = priority,
                TransactionIds = new List<long> { 1L }
            };

            SUT.QueueTiffMessage(actual);

            _queueService.Verify(_ => _.PublishQueueMessage(
            It.Is<QueueConfigurationSettings>(__ => __.MaxPriority == maxPriority),
            It.IsAny<string>(),
            scaledPriority),
            Times.Once);
        }

        [Theory]
        [MemberData(nameof(TransactionErrorTests))]
        public void ShouldThrowExceptionOnNullOrEmptyList(Type exception, List<long> transactionsList)
        {
            var actual = new TiffQueueMessage
            {
                Priority = 1,
                TransactionIds = transactionsList
            };

            Assert.Throws(exception, () => SUT.QueueTiffMessage(actual));
        }

        [Theory]
        [MemberData(nameof(TransactionTests))]
        public void ShouldLogOncePerEveryTransactionId(List<long> transactionsList, int logCount)
        {
            var actual = new TiffQueueMessage
            {
                Priority = 1,
                TransactionIds = transactionsList
            };

            SUT.QueueTiffMessage(actual);

            logger.Verify(_ => _.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Exactly(logCount));
        }

        public static IEnumerable<object[]> TransactionTests
        {
            get
            {
                var tests = new List<object[]>();

                for (int i = 1; i <= 5; i++)
                {
                    var list = new long[i];
                    Array.Fill(list, i, 0, i);

                    tests.Add(new object[] { list.ToList(), list.Length });
                }

                return tests;
            }
        }

        public static IEnumerable<object[]> TransactionErrorTests
        {
            get
            {
                yield return new object[] { typeof(ArgumentNullException), null };
                yield return new object[] { typeof(ArgumentException), new List<long>(0) };
            }
        }
    }
}
