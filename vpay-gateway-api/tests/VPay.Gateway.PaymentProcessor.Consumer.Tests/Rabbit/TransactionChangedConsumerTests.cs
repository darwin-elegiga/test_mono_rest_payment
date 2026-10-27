using FluentAssertions;
using FluentAssertions.Execution;
using GreenPipes;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.EventBus.Messages.Transactions;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Rabbit;
using Xunit;

namespace VPay.Gateway.PaymentProcessor.Consumer.Tests.Rabbit;

/// <summary>
/// Tests for the <see cref="TransactionChangedConsumer"/>
/// </summary>
public class TransactionChangedConsumerTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<TransactionChangedConsumer>> _loggerMock = new();
    private readonly Mock<IBus> _busMock = new(MockBehavior.Strict);
    private readonly Mock<ConsumeContext<TransactionChanged>> _consumeContextMock = new(MockBehavior.Strict);
    private readonly TransactionChangedConsumer _consumer;

    public TransactionChangedConsumerTests()
    {
        _consumer = new TransactionChangedConsumer(_mediatorMock.Object, _loggerMock.Object, _busMock.Object);
        _consumeContextMock
            .SetupGet(x => x.CancellationToken)
            .Returns(CancellationToken.None)
            .Verifiable();
    }

    public void Dispose()
    {
        _mediatorMock.VerifyAll();
        _busMock.VerifyAll();
        _loggerMock.VerifyAll();
        _consumeContextMock.VerifyAll();
    }

    public class Consume : TransactionChangedConsumerTests
    {
        private const string ClientCode = nameof(ClientCode);
        private const string PaymentId = nameof(PaymentId);

        public Consume() =>
            _consumeContextMock
                .SetupGet(x => x.Message)
                .Returns(new TransactionChanged
                {
                    ClientCode = ClientCode,
                    PaymentId = PaymentId
                })
                .Verifiable();

        [Fact]
        public async Task WhenExternalClientConfigurationIsNull_LogsAndReturns()
        {
            ExternalClientConfigurationQuery query = null;
            SetupExternalClientConfigurationQuery(null, x => query = x);

            await _consumer.Consume(_consumeContextMock.Object).ConfigureAwait(false);

            using (new AssertionScope())
            {
                VerifyExternalClientConfigurationQuery(query);
                VerifyInformationLogged();
            }
        }

        [Fact]
        public async Task WhenNoPaymentsToProcess_LogsAndReturns()
        {
            ExternalClientConfigurationQuery query = null;
            SetupExternalClientConfigurationQuery(new ExternalClientConfigurationDto(), x => query = x);
            SetupPaymentProcessQueueFindNextCommand(null);

            await _consumer.Consume(_consumeContextMock.Object).ConfigureAwait(false);

            using (new AssertionScope())
            {
                VerifyExternalClientConfigurationQuery(query);
                VerifyInformationLogged();
            }
        }

        [Fact]
        public async Task WithPayment_PublishesMessage()
        {
            ExternalClientConfigurationQuery query = null;
            SetupExternalClientConfigurationQuery(new ExternalClientConfigurationDto(), x => query = x);
            SetupPaymentProcessQueueFindNextCommand(
                new PaymentProcessQueue
                {
                    LastRequestedDateTime = DateTime.Today.AddDays(-1)
                });

            _busMock
                .Setup(x => x.Publish(It.Is<NotifyClientPaymentChanged>(notification =>
                        notification.ClientCode == ClientCode
                        && notification.PaymentId == PaymentId
                        && notification.RequestTime == DateTime.Today.AddDays(-1)),
                    It.IsAny<IPipe<PublishContext<NotifyClientPaymentChanged>>>(),
                    CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _consumer.Consume(_consumeContextMock.Object).ConfigureAwait(false);

            using (new AssertionScope())
            {
                VerifyExternalClientConfigurationQuery(query);
                VerifyInformationLogged();
            }
        }

        private void SetupExternalClientConfigurationQuery(
            ExternalClientConfigurationDto result,
            Action<ExternalClientConfigurationQuery> capture)
        {
            _mediatorMock
                .Setup(x => x.Send(Capture.With(new CaptureMatch<ExternalClientConfigurationQuery>(capture)), CancellationToken.None))
                .ReturnsAsync(result)
                .Verifiable();
        }

        private void SetupPaymentProcessQueueFindNextCommand(PaymentProcessQueue result) =>
            _mediatorMock
                .Setup(x => x.Send(
                    It.Is<PaymentProcessQueueFindNextCommand>(cmd => cmd.ClientCode == ClientCode && cmd.ClientPaymentId == PaymentId),
                    CancellationToken.None))
                .ReturnsAsync(result)
                .Verifiable();

        private static void VerifyExternalClientConfigurationQuery(ExternalClientConfigurationQuery query)
        {
            query.ClientCode.Should().Be(ClientCode);
            query.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        private void VerifyInformationLogged() =>
            _loggerMock.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }
}
