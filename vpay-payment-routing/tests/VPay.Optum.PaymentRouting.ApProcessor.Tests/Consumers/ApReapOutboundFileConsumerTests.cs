using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.PaymentRouting.ApProcessor.Constants;
using VPay.Optum.PaymentRouting.ApProcessor.Consumers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using Xunit;

namespace VPay.Optum.PaymentRouting.ApProcessor.Tests.Consumers
{
    public class ApReapOutboundFileConsumerTests
    {
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<ILogger<ApReapOutboundFileConsumer>> _loggerMock;
        private readonly ApReapOutboundFileConsumer _consumer;

        public ApReapOutboundFileConsumerTests()
        {
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _loggerMock = new Mock<ILogger<ApReapOutboundFileConsumer>>();
            _consumer = new ApReapOutboundFileConsumer(_publishEndpointMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Consume_RoutingType_FinSeg_Publishes_BuildOutboundFinSeg()
        {
            // Arrange
            var message = new ApReapOutboundFile
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "COMPLETED",
                InboundPaymentId = 456,
                InboundPaymentStatus = "APSENT",
                ClientCode = "CLIENT1",
                ApMessageType = MessageTypes.Ap,
                ApRoutingType = RoutingTypes.FinSeg
            };

            var contextMock = new Mock<ConsumeContext<ApReapOutboundFile>>();
            contextMock.SetupGet(c => c.Message).Returns(message);

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            _publishEndpointMock.Verify(
                p => p.Publish(It.Is<BuildOutboundFinSeg>(b =>
                    b.ApMessageTrackingId == message.ApMessageTrackingId &&
                    b.ClientCode == message.ClientCode
                ), default), Times.Once);
        }

        [Fact]
        public async Task Consume_RoutingType_Azure_MessageType_Ap_Publishes_BuildOutbound835()
        {
            // Arrange
            var message = new ApReapOutboundFile
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "COMPLETED",
                InboundPaymentId = 456,
                InboundPaymentStatus = "APSENT",
                ClientCode = "CLIENT2",
                ApMessageType = MessageTypes.Ap,
                ApRoutingType = RoutingTypes.Azure
            };

            var contextMock = new Mock<ConsumeContext<ApReapOutboundFile>>();
            contextMock.SetupGet(c => c.Message).Returns(message);

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            _publishEndpointMock.Verify(
                p => p.Publish(It.Is<BuildOutbound835>(b =>
                    b.ApMessageTrackingId == message.ApMessageTrackingId &&
                    b.ClientCode == message.ClientCode
                ), default), Times.Once);
        }

        [Fact]
        public async Task Consume_WhenRoutingTypeIsOpach_DoesNotPublish()
        {
            // Arrange
            var message = new ApReapOutboundFile
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "COMPLETED",
                InboundPaymentId = 456,
                InboundPaymentStatus = "APSENT",
                ClientCode = "CLIENT3",
                ApMessageType = MessageTypes.Ap,
                ApRoutingType = RoutingTypes.Opach
            };

            var contextMock = new Mock<ConsumeContext<ApReapOutboundFile>>();
            contextMock.SetupGet(c => c.Message).Returns(message);

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            _publishEndpointMock.Verify(p => p.Publish(It.IsAny<object>(), default), Times.Never);
        }

        [Fact]
        public async Task Consume_ExceptionIsThrown_ReThrowsException()
        {
            // Arrange
            var message = new ApReapOutboundFile
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "COMPLETED",
                InboundPaymentId = 456,
                InboundPaymentStatus = "APSENT",
                ClientCode = "CLIENT4",
                ApMessageType = MessageTypes.Ap,
                ApRoutingType = RoutingTypes.FinSeg
            };

            var contextMock = new Mock<ConsumeContext<ApReapOutboundFile>>();
            contextMock.SetupGet(c => c.Message).Returns(message);

            _publishEndpointMock
                .Setup(p => p.Publish(It.IsAny<BuildOutboundFinSeg>(), default))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _consumer.Consume(contextMock.Object));
        }
    }
}
