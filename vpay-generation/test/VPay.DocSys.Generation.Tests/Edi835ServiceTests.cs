using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;
using Xunit;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class Edi835ServiceTests
    {
        private readonly Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private readonly Mock<IQueueService> _queueService = new Mock<IQueueService>();
        private readonly ILogger<IMessagePublisher> _loggerMsgPublisher = new NullLogger<IMessagePublisher>();
        private readonly ILogger<Edi835Service> _logger = new NullLogger<Edi835Service>();
        private readonly QueueConfigurations _queueConfigurations = new QueueConfigurations();

        private Edi835Service _sut;

        public Edi835ServiceTests()
        {
            _queueConfigurations.Edi835GenerationQueue = new QueueConfigurationSettings();
        }

        #region PublishTransaction Tests
        
        [Fact]
        public async ValueTask PublishTransactions_Should_ReturnMessageQueued()
        {
            // Arrange
            var txnIds = new List<long> { 1 };
            var expectedEdi835 = new DS.Edi835
            {
                Edi835Id = 1,
                IsTerminalStatus = false,
                StatusCode = Edi835StatusCode.Initiated
            };
            var expectedResult = new Dictionary<long, string> { { 1, "Message queued & status updated to QueuedForGeneration." } };

            _edi835Repository
                .Setup(x => x.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEdi835);
            _edi835Repository
                .Setup(x => x.Add835Status(It.IsAny<long>(), It.IsAny<Edi835StatusCode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _queueService
                .Setup(x => x.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()))
                .Verifiable();
            _sut = new Edi835Service(_edi835Repository.Object, _queueConfigurations, _queueService.Object, _loggerMsgPublisher, _logger);

            // Act
            var result = await _sut.PublishTransactions(txnIds, "UnitTestRunner", "UnitTest", CancellationToken.None);

            // Assert
            result.Should().BeOfType<Dictionary<long, string>>().And.BeEquivalentTo(expectedResult);
            _queueService.Verify(x => x.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()));
        }

        [Fact]
        public async ValueTask PublishTransactions_Should_ReturnNotAnEdi835()
        {
            // Arrange
            var txnIds = new List<long> { 1 };
            var expectedResult = new Dictionary<long, string> { { 1, "Not an Edi835" } };

            _edi835Repository
                .Setup(x => x.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DS.Edi835)null);
            _sut = new Edi835Service(_edi835Repository.Object, _queueConfigurations, _queueService.Object, _loggerMsgPublisher, _logger);

            // Act
            var result = await _sut.PublishTransactions(txnIds, "UnitTestRunner", "UnitTest", CancellationToken.None);

            // Assert
            result.Should().BeOfType<Dictionary<long, string>>().And.BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async ValueTask PublishTransactions_Should_ReturnEdi835CannotBeQueued()
        {
            // Arrange
            var txnIds = new List<long> { 1 };
            var expectedEdi835 = new DS.Edi835
            {
                Edi835Id = 1,
                IsTerminalStatus = true,
                StatusCode = Edi835StatusCode.Initiated,
                StatusDescription = "ExpectedDescription"
            };
            var expectedResult = new Dictionary<long, string> { { 1, $"Edi835 cannot be queued.({expectedEdi835.StatusCode}:{expectedEdi835.StatusDescription})" } };

            _edi835Repository
                .Setup(x => x.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEdi835);
            _sut = new Edi835Service(_edi835Repository.Object, _queueConfigurations, _queueService.Object, _loggerMsgPublisher, _logger);

            // Act
            var result = await _sut.PublishTransactions(txnIds, "UnitTestRunner", "UnitTest", CancellationToken.None);

            // Assert
            result.Should().BeOfType<Dictionary<long, string>>().And.BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void PublishTransactions_Should_ReturnPartialResults_WhenExceptionThrown()
        {
            // Arrange
            var txnIds = new List<long> { 1 };
            var expectedException = new Exception("This is the expected Excpetion");
            var expectedEdi835 = new DS.Edi835
            {
                Edi835Id = 1,
                IsTerminalStatus = false,
                StatusCode = Edi835StatusCode.Initiated
            };
            var expectedResult = new Dictionary<long, string> { { 1, "Looking up Edi835" } };

            _edi835Repository
                .Setup(x => x.GetMostRecent835(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Throws(expectedException);
            _sut = new Edi835Service(_edi835Repository.Object, _queueConfigurations, _queueService.Object, _loggerMsgPublisher, _logger);

            // Act
            Func<Task> result = () => _sut.PublishTransactions(txnIds, "UnitTestRunner", "UnitTest", CancellationToken.None);

            // Assert
            result.Should().ThrowAsync<Exception>().Result.And.Data["PartialResults"].Should().BeEquivalentTo(expectedResult);                
        }

        #endregion

        #region QueueMessage Tests

        [Fact]
        public void QueueMessage_Should_Succeed()
        {
            // Arrange
            var expectedEdi835 = new DS.Edi835
            {
                Edi835Id = 1,
                IsTerminalStatus = false,
                StatusCode = Edi835StatusCode.Initiated
            };
            _queueService
                .Setup(x => x.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()))
                .Verifiable();
            _sut = new Edi835Service(_edi835Repository.Object, _queueConfigurations, _queueService.Object, _loggerMsgPublisher, _logger);

            // Act
            _sut.QueueMessage(expectedEdi835);

            // Assert
            _queueService.Verify(x => x.PublishQueueMessage(It.IsAny<QueueConfigurationSettings>(), It.IsAny<string>(), It.IsAny<byte>()));
        }

        [Fact]
        public void QueueMessage_Should_ThrowIfNullEdi835()
        {
            // Arrange
            _sut = new Edi835Service(_edi835Repository.Object, _queueConfigurations, _queueService.Object, _loggerMsgPublisher, _logger);

            // Act
            Action result = () => _sut.QueueMessage(null);

            // Assert
            result.Should().Throw<Exception>().And.Message.Should().Be("Value cannot be null. (Parameter 'edi835')");
        }

        #endregion
    }
}
