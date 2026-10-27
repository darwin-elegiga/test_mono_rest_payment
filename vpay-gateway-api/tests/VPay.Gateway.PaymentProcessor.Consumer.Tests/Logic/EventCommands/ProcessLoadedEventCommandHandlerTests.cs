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

public class ProcessLoadedEventCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator = new Mock<IMediator>(MockBehavior.Strict);
    private readonly ILogger<ProcessLoadedEventCommandHandler> _logger = new NullLogger<ProcessLoadedEventCommandHandler>();
    private readonly ProcessLoadedEventCommandHandler _handler;

    public ProcessLoadedEventCommandHandlerTests()
    {
        _handler = new ProcessLoadedEventCommandHandler(_mockMediator.Object, _logger);
    }

    public void Dispose()
    {
        _mockMediator.VerifyAll();
    }

    public class Handle : ProcessLoadedEventCommandHandlerTests
    {
        [Theory]
        [InlineData("CLCHK", "Payment Requested/Loaded - Check")]
        [InlineData("CLVCP", "Payment Requested/Loaded - Card")]
        [InlineData("CLEFT", "Payment Requested/Loaded - ACH")]
        [InlineData("CLSDA", "Payment Requested/Loaded - ACH")]
        [InlineData("CLDPY", "Payment Requested/Loaded - Direct Deposit")]
        [InlineData("CLNPF", "Payment Requested/Loaded - No Payment")]
        [InlineData("CLRAND", "Payment Requested/Loaded")]
        public async Task SuccessfulUpdateOfNoteWithNoLastSendStatusShouldAlsoUpdatePaymentStatusToIssuedAndReturnTrueWithNote(string paymentType, string expectedNote)
        {
            var command = GetLoadedEventCommand();
            command = GivenPaymentTypeIs(command, paymentType);
            command = GivenThePaymentStatusIsNotSet(command);
            var expectedPaymentStatus = nameof(GuidewirePaymentStatus.Requested);

            SendPaymentNoteCommand actCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                .ReturnsAsync(new GuidewireResult { Status = "SUCCESS" })
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, t) =>
                {
                    actCommand = (SendPaymentNoteCommand)q;
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

        [Theory]
        [InlineData("CLCHK", "Payment Requested/Loaded - Check")]
        [InlineData("CLVCP", "Payment Requested/Loaded - Card")]
        [InlineData("CLEFT", "Payment Requested/Loaded - ACH")]
        [InlineData("CLSDA", "Payment Requested/Loaded - ACH")]
        [InlineData("CLDPY", "Payment Requested/Loaded - Direct Deposit")]
        [InlineData("CLNPF", "Payment Requested/Loaded - No Payment")]
        [InlineData("CLRAND", "Payment Requested/Loaded")]
        public async Task SuccessfulUpdateOfNoteWithLastSendStatusSetShouldReturnTrueWithNoteButNotSendPaymentStatus(string paymentType, string expectedNote)
        {
            var command = GetLoadedEventCommand();
            command = GivenPaymentTypeIs(command, paymentType);
            command = GivenThePaymentStatusIsSet(command);
            var expectedPaymentStatus = command.PaymentStatus;

            SendPaymentNoteCommand actCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                .ReturnsAsync(new GuidewireResult { Status = "SUCCESS" })
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, t) =>
                {
                    actCommand = (SendPaymentNoteCommand)q;
                })
                .Verifiable();

            _mockMediator.Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(x => x.PaymentEventId == command.PaymentEvent.Id && x.Status == PaymentNotificationStatus.Completed.Name && x.PreviousStatus == command.PaymentEvent.CurrentStatus), default))
                .ReturnsAsync(command.PaymentEvent)
                .Verifiable();
            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
                actCommand.Should().BeEquivalentTo(new SendPaymentNoteCommand(command.ClientPaymentId, expectedNote, command.Settings));
            }
        }

        [Theory]
        [InlineData("CLCHK", "Payment Requested/Loaded - Check")]
        [InlineData("CLVCP", "Payment Requested/Loaded - Card")]
        [InlineData("CLEFT", "Payment Requested/Loaded - ACH")]
        [InlineData("CLSDA", "Payment Requested/Loaded - ACH")]
        [InlineData("CLDPY", "Payment Requested/Loaded - Direct Deposit")]
        [InlineData("CLNPF", "Payment Requested/Loaded - No Payment")]
        [InlineData("CLRAND", "Payment Requested/Loaded")]
        public async Task FailureToUpdateOfNoteThenUpdateEventStatusToFailed(string paymentType, string expectedNote)
        {
            var command = GetLoadedEventCommand();
            command = GivenPaymentTypeIs(command, paymentType);
            command = GivenThePaymentStatusIsSet(command);
            var expectedPaymentStatus = command.PaymentStatus;

            SendPaymentNoteCommand actCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                .ReturnsAsync(new GuidewireResult { Status = "FAILED" })
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, t) =>
                {
                    actCommand = (SendPaymentNoteCommand)q;
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

        private ProcessLoadedEventCommand GetLoadedEventCommand()
        {
            return new ProcessLoadedEventCommand(new PaymentEvent
                {
                Id = 10,
                PaymentId = 1,
                EventPaymentType = "RANDOM",
                EventStatus = "LOADED",
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
            , "PAYID:1"
            , ""
            );
        }

        private ProcessLoadedEventCommand GivenPaymentTypeIs(ProcessLoadedEventCommand command, string paymentType)
        {
            command.PaymentEvent.EventPaymentType = paymentType;
            return command;
        }

        private ProcessLoadedEventCommand GivenThePaymentStatusIsSet(ProcessLoadedEventCommand command)
        {
            return new ProcessLoadedEventCommand(command.PaymentEvent, command.Settings, command.ClientPaymentId, nameof(GuidewirePaymentStatus.Issued));
        }

        private ProcessLoadedEventCommand GivenThePaymentStatusIsNotSet(ProcessLoadedEventCommand command)
        {
            return new ProcessLoadedEventCommand(command.PaymentEvent, command.Settings, command.ClientPaymentId, "");
        }
    }
}
