using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.PaymentRouting.ApProcessor.Consumers;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;
using Xunit;

namespace VPay.Optum.PaymentRouting.ApProcessor.Tests.Consumers
{
    public class ApReapPaymentConsumerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IUserInfo> _userInfoMock;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<ILogger<ApReapPaymentConsumer>> _loggerMock;
        private readonly ApReapPaymentConsumer _consumer;

        public ApReapPaymentConsumerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _userInfoMock = new Mock<IUserInfo>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _loggerMock = new Mock<ILogger<ApReapPaymentConsumer>>();
            _userInfoMock.Setup(x => x.GetCurrentUserName()).Returns("TestUser");

            _consumer = new ApReapPaymentConsumer(
                _mediatorMock.Object,
                _userInfoMock.Object,
                _publishEndpointMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Consume_UpdatesPaymentStatus_AndPublishesOutboundFile()
        {
            // Arrange
            var message = new ApReapPayment
            {
                InboundPaymentId = 123,
                InboundPaymentStatus = "APSENT",
                ClientCode = "C1",
                BillingEntityCode = "B1",
                ApMessageType = "TypeA",
                ApRoutingType = "RouteA",
                IsOptumEnrolled = true,
                ApMessageTrackingId = 456,
                ApMessageTrackingStatus = "COMPLETED"
            };

            var contextMock = new Mock<ConsumeContext<ApReapPayment>>();
            contextMock.SetupGet(x => x.Message).Returns(message);
            contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateInboundPaymentStatusSql.Command>(), It.IsAny<CancellationToken>()));

            _publishEndpointMock
                .Setup(p => p.Publish(It.IsAny<ApReapOutboundFile>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<UpdateInboundPaymentStatusSql.Command>(cmd =>
                    cmd.InboundPaymentId == message.InboundPaymentId &&
                    cmd.Status.Name == message.InboundPaymentStatus &&
                    cmd.ModifiedBy == "TestUser"
                ),
                It.IsAny<CancellationToken>()), Times.Once);

            _publishEndpointMock.Verify(p => p.Publish(
                It.Is<ApReapOutboundFile>(f =>
                    f.ClientCode == message.ClientCode &&
                    f.BillingEntityCode == message.BillingEntityCode &&
                    f.ApMessageType == message.ApMessageType &&
                    f.ApRoutingType == message.ApRoutingType &&
                    f.IsOptumEnrolled == message.IsOptumEnrolled &&
                    f.InboundPaymentId == message.InboundPaymentId &&
                    f.InboundPaymentStatus == message.InboundPaymentStatus &&
                    f.ApMessageTrackingId == message.ApMessageTrackingId &&
                    f.ApMessageTrackingStatus == message.ApMessageTrackingStatus
                ),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Consume_WhenUpdatePaymentStatusThrows_ThrowsException()
        {
            // Arrange
            var message = new ApReapPayment
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "COMPLETED",
                InboundPaymentId = 456,
                InboundPaymentStatus = "APSENT"
            };

            var contextMock = new Mock<ConsumeContext<ApReapPayment>>();
            contextMock.SetupGet(x => x.Message).Returns(message);
            contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

            var exception = new Exception("DB error");
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateInboundPaymentStatusSql.Command>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _consumer.Consume(contextMock.Object));
            Assert.Equal("DB error", ex.Message);

            _publishEndpointMock.Verify(p => p.Publish(It.IsAny<ApReapOutboundFile>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Constructor_NullDependencies_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ApReapPaymentConsumer(null, _userInfoMock.Object, _publishEndpointMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() =>
                new ApReapPaymentConsumer(_mediatorMock.Object, null, _publishEndpointMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() =>
                new ApReapPaymentConsumer(_mediatorMock.Object, _userInfoMock.Object, null, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() =>
                new ApReapPaymentConsumer(_mediatorMock.Object, _userInfoMock.Object, _publishEndpointMock.Object, null));
        }
    }
}
