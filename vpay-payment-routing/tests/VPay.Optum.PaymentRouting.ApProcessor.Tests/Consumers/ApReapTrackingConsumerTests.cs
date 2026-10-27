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
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;
using Xunit;

namespace VPay.Optum.PaymentRouting.ApProcessor.Tests.Consumers
{
    public class ApReapTrackingConsumerTests
    {
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IUserInfo> _userInfoMock = new();
        private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
        private readonly Mock<ILogger<ApReapTrackingConsumer>> _loggerMock = new();

        private ApReapTrackingConsumer CreateConsumer(string userName = "TestUser")
        {
            _userInfoMock.Setup(x => x.GetCurrentUserName()).Returns(userName);
            return new ApReapTrackingConsumer(
                _mediatorMock.Object,
                _userInfoMock.Object,
                _publishEndpointMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Consume_UpdatesTrackingStatus_AndPublishesPayment()
        {
            // Arrange
            var consumer = CreateConsumer();
            var apMessage = new ApReapTracking
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "APSENT",
                ClientCode = "C1",
                BillingEntityCode = "B1",
                ApMessageType = "Type1",
                ApRoutingType = "Route1",
                IsOptumEnrolled = true,
                InboundPaymentId = 456,
                InboundPaymentStatus = "COMPLETED"
            };

            var contextMock = new Mock<ConsumeContext<ApReapTracking>>();
            contextMock.SetupGet(x => x.Message).Returns(apMessage);
            contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()));

            _publishEndpointMock
                .Setup(p => p.Publish(It.IsAny<ApReapPayment>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await consumer.Consume(contextMock.Object);

            // Assert
            _mediatorMock.Verify(m => m.Send(It.Is<UpdateApMessageTrackingStatus.Command>(cmd =>
                cmd.ApMessageTrackingId == apMessage.ApMessageTrackingId &&
                cmd.Status.Name == apMessage.ApMessageTrackingStatus &&
                cmd.ModifiedBy == "TestUser"
            ), It.IsAny<CancellationToken>()), Times.Once);

            _publishEndpointMock.Verify(p => p.Publish(It.Is<ApReapPayment>(payment =>
                payment.ApMessageTrackingId == apMessage.ApMessageTrackingId &&
                payment.ApMessageTrackingStatus == apMessage.ApMessageTrackingStatus
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Consume_Throws_OnException()
        {
            // Arrange
            var consumer = CreateConsumer();
            var apMessage = new ApReapTracking
            {
                ApMessageTrackingId = 123,
                ApMessageTrackingStatus = "APSENT",
                InboundPaymentId = 456,
                InboundPaymentStatus = "COMPLETED"
            };

            var contextMock = new Mock<ConsumeContext<ApReapTracking>>();
            contextMock.SetupGet(x => x.Message).Returns(apMessage);
            contextMock.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateApMessageTrackingStatus.Command>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(contextMock.Object));
        }

        [Fact]
        public async Task UpdateTrackingStatus_CallsMediatorSend_WithCorrectParameters()
        {
            // Arrange
            var consumer = CreateConsumer("TestUser2");
            var status = ApMessageTrackingStatus.FromName("APSENT");

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()));

            // Act
            await consumer.UpdateTrackingStatus(42, status);

            // Assert
            _mediatorMock.Verify(m => m.Send(It.Is<UpdateApMessageTrackingStatus.Command>(cmd =>
                cmd.ApMessageTrackingId == 42 &&
                cmd.Status == status &&
                cmd.ModifiedBy == "TestUser2"
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDependenciesAreNull()
        {
            _userInfoMock.Setup(x => x.GetCurrentUserName()).Returns("TestUser");

            Assert.Throws<ArgumentNullException>(() => new ApReapTrackingConsumer(null, _userInfoMock.Object, _publishEndpointMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new ApReapTrackingConsumer(_mediatorMock.Object, null, _publishEndpointMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new ApReapTrackingConsumer(_mediatorMock.Object, _userInfoMock.Object, null, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new ApReapTrackingConsumer(_mediatorMock.Object, _userInfoMock.Object, _publishEndpointMock.Object, null));
        }
    }
}
