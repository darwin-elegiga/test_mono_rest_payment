using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Logic;
using VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;
using Xunit;

namespace VPay.Gateway.PaymentProcessor.Consumer.Tests.Logic.EventCommands;

public class ProcessPurchaseEventCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator = new Mock<IMediator>(MockBehavior.Strict);
    private readonly ILogger<ProcessPurchaseEventCommandHandler> _logger = new NullLogger<ProcessPurchaseEventCommandHandler>();
    private readonly ProcessPurchaseEventCommandHandler _handler;

    public ProcessPurchaseEventCommandHandlerTests()
    {
        _handler = new ProcessPurchaseEventCommandHandler(_mockMediator.Object, _logger);
    }

    public void Dispose()
    {
        _mockMediator.VerifyAll();
    }

    public class Handle : ProcessPurchaseEventCommandHandlerTests
    {
        [Fact]
        public async Task WhenCurrentBalanceIs0ThenWillUpdateToClearedAndRecordIfSuccessfullyUpdated()
        {
            var command = GetPurchaseEventCommand();
            command = GivenTheCurrentBalanceIs(command, 0);
            var expectedPaymentStatus = nameof(GuidewirePaymentStatus.Cleared);
            var expectedNote = "Payment Settled";

            SendPaymentStatusChangeCommand actCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(new GuidewireResult { Status = "SUCCESS" })
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, t) =>
                {
                    actCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(x => x.PaymentEventId == command.PaymentEvent.Id && x.Status == PaymentNotificationStatus.Completed.Name && x.PreviousStatus == command.PaymentEvent.CurrentStatus), default))
                .ReturnsAsync(command.PaymentEvent)
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<PaymentUpdateStatusCommand>(x => x.PaymentId == command.PaymentEvent.PaymentId && x.Status == expectedPaymentStatus && x.PreviousStatus == command.PaymentStatus), default))
                .ReturnsAsync(new Payment { LastSentStatus = expectedPaymentStatus })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
                actCommand.Should().BeEquivalentTo(new SendPaymentNoteCommand(command.ClientPaymentId, expectedNote, command.Settings));
            }
        }

        [Fact]
        public async Task WhenCurrentBalanceIs0AndItTryToUpdateToClearedfFailedThenEventStatusShouldBeUpdatedToFailed()
        {
            var command = GetPurchaseEventCommand();
            command = GivenTheCurrentBalanceIs(command, 0);
            var expectedPaymentStatus = command.PaymentStatus;
            var expectedNote = "Payment Settled";
            var guidewireResult = new GuidewireResult { Status = "FAILED" };

            SendPaymentStatusChangeCommand actCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(guidewireResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, t) =>
                {
                    actCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(x => x.PaymentEventId == command.PaymentEvent.Id && x.Status == PaymentNotificationStatus.Failed.Name && x.PreviousStatus == command.PaymentEvent.CurrentStatus), default))
                .ReturnsAsync(command.PaymentEvent)
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(false, expectedPaymentStatus));
                actCommand.Should().BeEquivalentTo(new SendPaymentNoteCommand(command.ClientPaymentId, expectedNote, command.Settings));
            }
        }

        [Fact]
        public async Task WhenCurrentBalanceIsNot0ThenWillMarkEventToAsSkipped()
        {
            var command = GetPurchaseEventCommand();
            command = GivenTheCurrentBalanceIs(command, 10m);
            var expectedPaymentStatus = command.PaymentStatus;

            _mockMediator.Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(x => x.PaymentEventId == command.PaymentEvent.Id && x.Status == PaymentNotificationStatus.Skipped.Name && x.PreviousStatus == command.PaymentEvent.CurrentStatus), default))
                .ReturnsAsync(command.PaymentEvent)
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
            }
        }

        private static ProcessPurchaseEventCommand GetPurchaseEventCommand()
        {
            return new ProcessPurchaseEventCommand(new PaymentEvent
                {
                Id = 10,
                PaymentId = 1,
                EventPaymentType = "RANDOM",
                EventStatus = "PURCHASE",
                EventDateTime = DateTime.Now.AddDays(-1),
                EventReasonCode = "",
                EventReasonDescription = "",
                CurrentStatus = PaymentNotificationStatus.Scheduled.Name
            },
            new Common.Guidewire.Models.GuidewireSettings
            {
                Url = "http://temp.com",
                UserName = "sa",
                Password = "pass"
            }
            , "CBC"
            , "PAYID:1"
            , nameof(GuidewirePaymentStatus.Issued)
            , 0m);
        }

        private static ProcessPurchaseEventCommand GivenTheCurrentBalanceIs(ProcessPurchaseEventCommand command, decimal currentBalance)
        {
            return new ProcessPurchaseEventCommand(command.PaymentEvent, command.Settings, command.ClientCode, command.ClientPaymentId, nameof(GuidewirePaymentStatus.Issued), currentBalance);
        }
    }
}
