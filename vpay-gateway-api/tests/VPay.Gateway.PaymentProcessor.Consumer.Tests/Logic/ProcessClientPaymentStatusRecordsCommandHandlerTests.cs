using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Logic;
using VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;
using Xunit;

namespace VPay.Gateway.PaymentProcessor.Consumer.Tests.Logic;

/// <summary>
/// Tests for the <see cref="ProcessClientPaymentStatusRecordsCommandHandler"/>
/// </summary>
public class ProcessClientPaymentStatusRecordsCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
    private readonly ProcessClientPaymentStatusRecordsCommandHandler _handler;

    protected ProcessClientPaymentStatusRecordsCommandHandlerTests() =>
        _handler = new ProcessClientPaymentStatusRecordsCommandHandler(_mediatorMock.Object, new NullLogger<ProcessClientPaymentStatusRecordsCommandHandler>());

    public void Dispose() =>
        _mediatorMock.VerifyAll();

    public class Handle : ProcessClientPaymentStatusRecordsCommandHandlerTests
    {
        private const string ClientCode = "Client";
        private const string PaymentId = "PaymentId";

        private readonly ExternalClientConfigurationDto _externalClientConfiguration = new()
        {
            Url = "https://test.com",
            Username = "SomeUser",
            Password = "SuperSecretPassword"
        };

        [Fact]
        public async Task WhenPaymentNotFound_ReturnsFalse()
        {
            SetupPaymentQuery();

            var result = await _handler.Handle(new ProcessClientPaymentStatusRecordsCommand(ClientCode, PaymentId), CancellationToken.None).ConfigureAwait(false);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task WhenExternalClientConfigurationNotFound_ReturnsFalse()
        {
            SetupPaymentQuery(payment: new Payment());
            SetupExternalClientConfigurationQuery();

            var result = await _handler.Handle(new ProcessClientPaymentStatusRecordsCommand(ClientCode, PaymentId), CancellationToken.None).ConfigureAwait(false);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task WithNoPaymentEvents_ReturnsTrue()
        {
            SetupPaymentQuery(payment: new Payment());
            SetupExternalClientConfigurationQuery(configuration: _externalClientConfiguration);
            SetupPaymentEventsQuery();

            var result = await _handler.Handle(new ProcessClientPaymentStatusRecordsCommand(ClientCode, PaymentId), CancellationToken.None).ConfigureAwait(false);
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("STOPPAY")]
        [InlineData("STOPPAY_R")]
        [InlineData("UNLOADS")]
        [InlineData("UNLOADSF")]
        [InlineData("UNLOAD")]
        public async Task WithUnloadEvent_ProcessesUnloadEvent(string eventStatus)
        {
            var paymentEvent = new PaymentEvent
            {
                EventStatus = eventStatus
            };

            SetupPaymentQuery(payment: new Payment { LastSentStatus = "TestStatus" });
            SetupExternalClientConfigurationQuery(configuration: _externalClientConfiguration);
            SetupPaymentEventsQuery(paymentEvents: new[] { paymentEvent });

            _mediatorMock
                .Setup(x => x.Send(It.Is<ProcessUnloadEventCommand>(cmd =>
                        cmd.PaymentEvent == paymentEvent
                        && cmd.ClientCode == ClientCode
                        && cmd.ClientPaymentId == PaymentId
                        && cmd.PaymentStatus == "TestStatus"),
                    CancellationToken.None))
                .ReturnsAsync(new PaymentEventProcessResult(true, "SUCCESS"))
                .Verifiable();

            var result = await _handler.Handle(new ProcessClientPaymentStatusRecordsCommand(ClientCode, PaymentId), CancellationToken.None).ConfigureAwait(false);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WithUnloadRejStatus_ShouldBeSkipped()
        {
            var paymentEvent = new PaymentEvent
            {
                EventStatus = "UNLOAD_REJ",
                CurrentStatus = "Current"
            };

            SetupPaymentQuery(payment: new Payment { LastSentStatus = "TestStatus" });
            SetupExternalClientConfigurationQuery(configuration: _externalClientConfiguration);
            SetupPaymentEventsQuery(paymentEvents: new[] { paymentEvent });

            _mediatorMock
                .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(cmd =>
                        cmd.PaymentEventId == paymentEvent.Id
                        && cmd.Status == PaymentNotificationStatus.Skipped.Name
                        && cmd.PreviousStatus == paymentEvent.CurrentStatus),
                    CancellationToken.None))
                .ReturnsAsync(new PaymentEvent())
                .Verifiable();

            var result = await _handler.Handle(new ProcessClientPaymentStatusRecordsCommand(ClientCode, PaymentId), CancellationToken.None).ConfigureAwait(false);
            result.Should().BeTrue();
        }

        private void SetupPaymentQuery(string clientCode = ClientCode, string paymentId = PaymentId, Payment payment = null) =>
            _mediatorMock
                .Setup(x => x.Send(It.Is<PaymentQuery>(q =>
                        q.ClientCode == clientCode
                        && q.ClientPaymentId == paymentId),
                    CancellationToken.None))
                .ReturnsAsync(() =>
                {
                    if (payment is not null)
                    {
                        payment.ClientCode = clientCode;
                        payment.ClientPaymentId = paymentId;
                    }

                    return payment;
                })
                .Verifiable();

        private void SetupPaymentEventsQuery(string clientCode = ClientCode, string paymentId = PaymentId, IReadOnlyCollection<PaymentEvent> paymentEvents = null) =>
            _mediatorMock
                .Setup(x => x.Send(It.Is<PaymentEventsQuery>(q =>
                        q.ClientCode == clientCode
                        && q.ClientPaymentId == paymentId),
                    CancellationToken.None))
                .ReturnsAsync(paymentEvents ?? Array.Empty<PaymentEvent>())
                .Verifiable();

        private void SetupExternalClientConfigurationQuery(string clientCode = ClientCode, ExternalClientConfigurationDto configuration = null) =>
            _mediatorMock
                .Setup(x => x.Send(It.Is<ExternalClientConfigurationQuery>(q => q.ClientCode == clientCode),
                    CancellationToken.None))
                .ReturnsAsync(configuration)
                .Verifiable();
    }
}
