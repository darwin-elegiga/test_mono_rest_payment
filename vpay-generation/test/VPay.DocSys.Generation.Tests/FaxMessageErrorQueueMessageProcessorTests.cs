using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Moq;

using Newtonsoft.Json;

using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class FaxMessageErrorQueueMessageProcessorTests
    {
        private static int maxRetryCount = 4;

        private IFaxMessageErrorQueueMessageProcessor SUT;

        private QueueConfigurations _queueConfigurations;
        private Mock<IQueueService> _queueService = new Mock<IQueueService>();
        private Mock<ILogger<FaxMessageErrorQueueMessageProcessor>> _logger = new Mock<ILogger<FaxMessageErrorQueueMessageProcessor>>();

        public FaxMessageErrorQueueMessageProcessorTests()
        {
            _queueConfigurations = new QueueConfigurations
            {
                FaxMessageQueue = new QueueConfigurationSettings
                {
                    QueueName = "TEST",
                    ErrorQueueName = "ERROR_TEST",
                    MaxPriority = 100
                }
            };

            _queueService.Invocations.Clear();
            _logger.Invocations.Clear();

            _logger.Setup(_ => _.Log(LogLevel.Critical, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()));

            _queueService
                .Setup(_ => _.GetErrorQueueConfiguration(It.IsAny<QueueConfigurationSettings>()))
                .Returns((QueueConfigurationSettings a) => new QueueConfigurationSettings
                {
                    QueueName = a.ErrorQueueName
                });

            SUT = new FaxMessageErrorQueueMessageProcessor(
                _queueConfigurations,
                _queueService.Object,
                _logger.Object);
        }

        [MemberData(nameof(GetRetryData))]
        [Theory]
        public void ShouldPublishToDifferentQueueForRetryCountUnderMaxRetryCount(int retryCount, FaxMessageIndex index)
        {
            var message = GetPayload(retryCount, index);
            SUT.ProcessMessageString(message, maxRetryCount);

            _queueService.Verify(_ => _.GetErrorQueueConfiguration(It.IsAny<QueueConfigurationSettings>()), Times.Once);
            _queueService.Verify(_ => _.PublishQueueMessage(It.Is<QueueConfigurationSettings>(
                    __ => __.QueueName == _queueConfigurations.FaxMessageQueue.QueueName),
                It.IsAny<string>(), It.IsAny<byte>()), Times.Once);
            _queueService.Verify(_ => _.PublishQueueMessage(It.Is<QueueConfigurationSettings>(
                    __ => __.QueueName == _queueConfigurations.FaxMessageQueue.ErrorQueueName),
                It.IsAny<string>(), 0), Times.Never);
        }

        [MemberData(nameof(GetNonRetryData))]
        [Theory]
        public void ShouldKeepInSameQueueForRetryCountOverMaxRetryCount(int retryCount, FaxMessageIndex index)
        {
            var message = GetPayload(retryCount, index);
            SUT.ProcessMessageString(message, maxRetryCount);

            _queueService.Verify(_ => _.GetErrorQueueConfiguration(It.IsAny<QueueConfigurationSettings>()), Times.Once);
            _queueService.Verify(_ => _.PublishQueueMessage(It.Is<QueueConfigurationSettings>(
                    __ => __.QueueName == _queueConfigurations.FaxMessageQueue.QueueName),
                It.IsAny<string>(), 0), Times.Never);
            _queueService.Verify(_ => _.PublishQueueMessage(It.Is<QueueConfigurationSettings>(
                    __ => __.QueueName == _queueConfigurations.FaxMessageQueue.ErrorQueueName),
                It.IsAny<string>(), 0), Times.Once);
        }

        [Theory]
        [InlineData(1, 3)] // will be not over max retry count
        [InlineData(5, 6)] // will be equal to max retry count
        public void ShouldLogInformationForRetryCountUnderOrEqualToMax(int retryCount, int maxRetry)
        {
            var message = GetPayload(retryCount, FaxMessageIndex.CreateFax);
            SUT.ProcessMessageString(message, maxRetry);

            _logger.Verify(_ => _.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Theory]
        [InlineData(6, 2)] // will be over max retry count
        [InlineData(4, 3)] // will be over max retry count
        [InlineData(5, 5)] // will be over max retry count
        public void ShouldNotLogForRetryCountOverMax(int retryCount, int maxRetry)
        {
            var message = GetPayload(retryCount, FaxMessageIndex.CreateFax);
            SUT.ProcessMessageString(message, maxRetry);

            _logger.Verify(_ => _.Log(It.Is<LogLevel>(l => l != LogLevel.Debug), It.IsAny<EventId>(), It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()), Times.Never);
        }

        [Fact]
        public void ShouldWrapAndSetMaxRetryCountForBadPayload()
        {
            // no xml, after pay, uses : instead of ,
            var payload = "{\"tid\":\"00000000000003491276\",\"cid\":\"00000000000001082260\",\"pay\":\"CLVCP\": \"ori\":\"SE-WAR\": \"tpa\":\"WMC\": \"be\":\"WMC\"}";
            var newPayload = JsonConvert.SerializeObject(new
            {
                ErrorRetryCount = 4, // one more that configuration value of 3
                BadPayload = JsonConvert.SerializeObject(payload)
            }, Formatting.Indented);

            SUT.ProcessMessageString($"{new string(' ', 275)}{payload}", maxRetryCount);

            _queueService.Verify(_ => _.PublishQueueMessage(It.Is<QueueConfigurationSettings>(
                    __ => __.QueueName == _queueConfigurations.FaxMessageQueue.ErrorQueueName),
                $"{new string(' ', 275)}{newPayload}", 0), Times.Once);
        }

        public static IEnumerable<object[]> GetRetryData
        {
            get
            {
                var list = new List<object[]>();

                // we stop at <, since it will ++ inside of the ProcessMessageString function
                for (var i = 0; i < maxRetryCount; i++)
                {
                    list.AddRange(
                        from FaxMessageIndex e
                        in Enum.GetValues(typeof(FaxMessageIndex))
                        select new object[] { i, e });
                }

                return list;
            }
        }

        public static IEnumerable<object[]> GetNonRetryData
        {
            get
            {
                var list = new List<object[]>();

                // we start at =, since it will ++ inside of the ProcessMessageString function
                for (var i = maxRetryCount; i < maxRetryCount * 2; i++)
                {
                    list.AddRange(
                        from FaxMessageIndex e
                        in Enum.GetValues(typeof(FaxMessageIndex))
                        select new object[] { i, e });
                }

                return list;
            }
        }

        private static string GetPayload(int retryCount, FaxMessageIndex type)
        {
            object payload;

            switch (type)
            {
                case FaxMessageIndex.CreateFax:
                    payload = new FaxMessagePayload
                    {
                        ErrorRetryCount = retryCount
                    };
                    break;

                case FaxMessageIndex.CreateEftFax:
                    payload = new FaxMessagePayload
                    {
                        ErrorRetryCount = retryCount
                    };
                    break;
                default:
                    throw new ArgumentException($"Need to handle {type}");
            }

            return $"{new string(' ', 73)}0000000001{new string(' ', 192)}{JsonConvert.SerializeObject(payload)}";
        }
    }
}
