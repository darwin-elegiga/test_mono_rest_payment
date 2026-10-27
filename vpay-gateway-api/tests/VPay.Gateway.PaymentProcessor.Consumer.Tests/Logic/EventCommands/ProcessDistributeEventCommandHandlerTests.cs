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

public class ProcessDistributeEventCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mockMediator = new Mock<IMediator>(MockBehavior.Strict);
    private readonly ILogger<ProcessDistributeEventCommandHandler> _logger = new NullLogger<ProcessDistributeEventCommandHandler>();
    private readonly ProcessDistributeEventCommandHandler _handler;

    public ProcessDistributeEventCommandHandlerTests()
    {
        _handler = new ProcessDistributeEventCommandHandler(_mockMediator.Object, _logger);
    }

    public void Dispose()
    {
        _mockMediator.VerifyAll();
    }

    public class Handle : ProcessDistributeEventCommandHandlerTests
    {
        [Fact]
        public async Task ForCLNPFWhenThePaymentIsAlreadyInIssuedStatusThenItShouldUpdateStatusToClearedAndRecordIfSuccessful()
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLNPF");
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Issued);
            var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
            var expectedPaymentStatus = nameof(GuidewirePaymentStatus.Cleared);
            var expectedPaymentStatusCodeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Cleared, command.ClientPaymentId, "Payment Settled", command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, command.PaymentEvent.CurrentStatus);
            var expectedPaymentUpdateStatusCommand = new PaymentUpdateStatusCommand(command.PaymentEvent.PaymentId, expectedPaymentStatus, command.PaymentStatus);

            SendPaymentStatusChangeCommand actPaymentStatusCodeCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            PaymentUpdateStatusCommand actPaymentUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentUpdateStatusCommand>(), default))
                .ReturnsAsync(new Payment { LastSentStatus = expectedPaymentStatus })
                .Callback<IRequest<Payment>, CancellationToken>((q, _) =>
                {
                    actPaymentUpdateStatusCommand = (PaymentUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
                actPaymentUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentUpdateStatusCommand);
            }
        }

        [Fact]
        public async Task ForCLNPFWhenThePaymentIsAlreadyInIssuedStatusAndFailsUpdateToStatusThenItShouldRecordStatusAsFailed()
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLNPF");
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Issued);
            var paymentUpdateResult = new GuidewireResult { Status = "FAILED" };
            var expectedPaymentStatus = command.PaymentStatus;
            var expectedPaymentStatusCodeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Cleared, command.ClientPaymentId, "Payment Settled", command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, command.PaymentEvent.CurrentStatus);

            SendPaymentStatusChangeCommand actPaymentStatusCodeCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(false, expectedPaymentStatus));
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
            }
        }

        [Theory]
        [InlineData("DISTFAX", "Payment Faxed", "")]
        [InlineData("DISTUSPS", "Payment Mailed - USPS", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx - Tracking Number: 123456", "123456")]
        [InlineData("DIST835PTL", "Payment Posted", "")]
        public async Task ForCLNPFWhenThenPaymentIsInRequestedStatusThenItShouldUpdateStatusToIssuedWithNoteForEventStatusTypeThenUpdateStatusToClearedAndRecordIfSuccessful(string eventStatusType, string note, string trackingNumber)
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLNPF");
            command = GivenPaymentEventStatusIs(command, eventStatusType);
            command = GivenPaymentEventTrackingNumberIs(command, trackingNumber);
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Requested);

            var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
            var expectedPaymentStatus = nameof(GuidewirePaymentStatus.Cleared);
            var expectedPaymentStatusCodeCommandIssued = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Issued, command.ClientPaymentId, note, command.Settings);
            var expectedPaymentStatusCodeCommandCleared = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Cleared, command.ClientPaymentId, "Payment Settled", command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, command.PaymentEvent.CurrentStatus);
            var expectedPaymentUpdateStatusCommandIssued = new PaymentUpdateStatusCommand(command.PaymentEvent.PaymentId, nameof(GuidewirePaymentStatus.Issued), command.PaymentStatus);
            var expectedPaymentUpdateStatusCommandCleared = new PaymentUpdateStatusCommand(command.PaymentEvent.PaymentId, expectedPaymentStatus, nameof(GuidewirePaymentStatus.Issued));

            SendPaymentStatusChangeCommand actPaymentStatusCodeCommandIssued = null;
            _mockMediator.Setup(x => x.Send(It.Is<SendPaymentStatusChangeCommand>(x => x.PaymentStatus == GuidewirePaymentStatus.Issued), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommandIssued = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();
            SendPaymentStatusChangeCommand actPaymentStatusCodeCommandCleared = null;
            _mockMediator.Setup(x => x.Send(It.Is<SendPaymentStatusChangeCommand>(x => x.PaymentStatus == GuidewirePaymentStatus.Cleared), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommandCleared = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            PaymentUpdateStatusCommand actPaymentUpdateStatusCommandIssued = null;
            _mockMediator.Setup(x => x.Send(It.Is<PaymentUpdateStatusCommand>(x => x.Status == nameof(GuidewirePaymentStatus.Issued)), default))
                .ReturnsAsync(new Payment { LastSentStatus = nameof(GuidewirePaymentStatus.Issued), ClientPaymentId = command.ClientPaymentId })
                .Callback<IRequest<Payment>, CancellationToken>((q, _) =>
                {
                    actPaymentUpdateStatusCommandIssued = (PaymentUpdateStatusCommand)q;
                })
                .Verifiable();

            PaymentUpdateStatusCommand actPaymentUpdateStatusCommandCleared = null;
            _mockMediator.Setup(x => x.Send(It.Is<PaymentUpdateStatusCommand>(x => x.Status == nameof(GuidewirePaymentStatus.Cleared)), default))
                .ReturnsAsync(new Payment { LastSentStatus = nameof(GuidewirePaymentStatus.Cleared), ClientPaymentId = command.ClientPaymentId })
                .Callback<IRequest<Payment>, CancellationToken>((q, _) =>
                {
                    actPaymentUpdateStatusCommandCleared = (PaymentUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
                actPaymentStatusCodeCommandIssued.Should().BeEquivalentTo(expectedPaymentStatusCodeCommandIssued);
                actPaymentStatusCodeCommandCleared.Should().BeEquivalentTo(expectedPaymentStatusCodeCommandCleared);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
                actPaymentUpdateStatusCommandIssued.Should().BeEquivalentTo(expectedPaymentUpdateStatusCommandIssued);
                actPaymentUpdateStatusCommandCleared.Should().BeEquivalentTo(expectedPaymentUpdateStatusCommandCleared);
            }
        }

        [Theory]
        [InlineData("DISTFAX", "Payment Faxed", "")]
        [InlineData("DISTUSPS", "Payment Mailed - USPS", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx - Tracking Number: 123456", "123456")]
        [InlineData("DIST835PTL", "Payment Posted", "")]
        public async Task ForNonCLNPFWhenThenPaymentIsInRequestedStatusThenItShouldUpdateStatusWithNoteForEventStatusTypeToIssueAndRecordIfSuccessful(string eventStatusType, string note, string trackingNumber)
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLVCP");
            command = GivenPaymentEventStatusIs(command, eventStatusType);
            command = GivenPaymentEventTrackingNumberIs(command, trackingNumber);
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Requested);

            var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
            var expectedPaymentStatus = nameof(GuidewirePaymentStatus.Issued);
            var expectedPaymentStatusCodeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Issued, command.ClientPaymentId, note, command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, command.PaymentEvent.CurrentStatus);
            var expectedPaymentUpdateStatusCommand = new PaymentUpdateStatusCommand(command.PaymentEvent.PaymentId, expectedPaymentStatus, command.PaymentStatus);

            SendPaymentStatusChangeCommand actPaymentStatusCodeCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            PaymentUpdateStatusCommand actPaymentUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentUpdateStatusCommand>(), default))
                .ReturnsAsync(new Payment { LastSentStatus = expectedPaymentStatus })
                .Callback<IRequest<Payment>, CancellationToken>((q, _) =>
                {
                    actPaymentUpdateStatusCommand = (PaymentUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
                actPaymentUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentUpdateStatusCommand);
            }
        }

        [Theory]
        [InlineData("DISTFAX", "Payment Faxed", "")]
        [InlineData("DISTUSPS", "Payment Mailed - USPS", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx - Tracking Number: 123456", "123456")]
        [InlineData("DIST835PTL", "Payment Posted", "")]
        public async Task ForNonCLNPFWhenThenPaymentIsInRequestedStatusAndFailsUpdateThenItShouldRecordFailedUpdateAndReturnFalse(string eventStatusType, string note, string trackingNumber)
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLVCP");
            command = GivenPaymentEventStatusIs(command, eventStatusType);
            command = GivenPaymentEventTrackingNumberIs(command, trackingNumber);
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Requested);

            var paymentUpdateResult = new GuidewireResult { Status = "FAILED" };
            var expectedPaymentStatus = command.PaymentStatus;
            var expectedPaymentStatusCodeCommand = new SendPaymentStatusChangeCommand(GuidewirePaymentStatus.Issued, command.ClientPaymentId, note, command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, command.PaymentEvent.CurrentStatus);

            SendPaymentStatusChangeCommand actPaymentStatusCodeCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentStatusChangeCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentStatusChangeCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(false, expectedPaymentStatus));
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
            }
        }

        [Theory]
        [InlineData("DISTFAX", "Payment Faxed", "")]
        [InlineData("DISTUSPS", "Payment Mailed - USPS", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx - Tracking Number: 123456", "123456")]
        [InlineData("DIST835PTL", "Payment Posted", "")]
        public async Task ForNonCLNPFWhenThenPaymentIsInIssuedStatusThenItShouldOnlySendNoteForEventStatusRecordIfSuccessful(string eventStatusType, string note, string trackingNumber)
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLVCP");
            command = GivenPaymentEventStatusIs(command, eventStatusType);
            command = GivenPaymentEventTrackingNumberIs(command, trackingNumber);
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Issued);

            var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
            var expectedPaymentStatus = nameof(GuidewirePaymentStatus.Issued);
            var expectedPaymentStatusCodeCommand = new SendPaymentNoteCommand(command.ClientPaymentId, note, command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Completed.Name, command.PaymentEvent.CurrentStatus);

            SendPaymentNoteCommand actPaymentStatusCodeCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentNoteCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(true, expectedPaymentStatus));
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
            }
        }

        [Theory]
        [InlineData("DISTFAX", "Payment Faxed", "")]
        [InlineData("DISTUSPS", "Payment Mailed - USPS", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx", "")]
        [InlineData("DISTFEDEX", "Payment Mailed - FedEx - Tracking Number: 123456", "123456")]
        [InlineData("DIST835PTL", "Payment Posted", "")]
        public async Task ForNonCLNPFWhenThenPaymentIsInIssuedStatusAndFailsUpdateThenItShouldRecordFailedUpdateAndReturnFalse(string eventStatusType, string note, string trackingNumber)
        {
            var command = GetEventCommand();
            command = GivenPaymentTypeIs(command, "CLVCP");
            command = GivenPaymentEventStatusIs(command, eventStatusType);
            command = GivenPaymentEventTrackingNumberIs(command, trackingNumber);
            command = GivenThePaymentStatusIsSet(command, GuidewirePaymentStatus.Issued);

            var paymentUpdateResult = new GuidewireResult { Status = "FAILED" };
            var expectedPaymentStatus = command.PaymentStatus;
            var expectedPaymentStatusCodeCommand = new SendPaymentNoteCommand(command.ClientPaymentId, note, command.Settings);
            var expectedPaymentEventUpdateStatusCommand = new PaymentEventUpdateStatusCommand(command.PaymentEvent.Id, PaymentNotificationStatus.Failed.Name, command.PaymentEvent.CurrentStatus);

            SendPaymentNoteCommand actPaymentStatusCodeCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                .ReturnsAsync(paymentUpdateResult)
                .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                {
                    actPaymentStatusCodeCommand = (SendPaymentNoteCommand)q;
                })
                .Verifiable();

            PaymentEventUpdateStatusCommand actPaymentEventUpdateStatusCommand = null;
            _mockMediator.Setup(x => x.Send(It.IsAny<PaymentEventUpdateStatusCommand>(), default))
                .ReturnsAsync(command.PaymentEvent)
                .Callback<IRequest<PaymentEvent>, CancellationToken>((q, _) =>
                {
                    actPaymentEventUpdateStatusCommand = (PaymentEventUpdateStatusCommand)q;
                })
                .Verifiable();

            var act = await _handler.Handle(command, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                act.Should().BeEquivalentTo(new PaymentEventProcessResult(false, expectedPaymentStatus));
                actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                actPaymentEventUpdateStatusCommand.Should().BeEquivalentTo(expectedPaymentEventUpdateStatusCommand);
            }
        }

        private ProcessDistributeEventCommand GetEventCommand()
        {
            return new ProcessDistributeEventCommand(new PaymentEvent
                {
                Id = 10,
                PaymentId = 1,
                EventPaymentType = "RANDOM",
                EventStatus = "DIST",
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
            , nameof(GuidewirePaymentStatus.Requested)
            );
        }

        private ProcessDistributeEventCommand GivenPaymentEventStatusIs(ProcessDistributeEventCommand command, string paymentEventStatus)
        {
            command.PaymentEvent.EventStatus = paymentEventStatus;
            return command;
        }

        private ProcessDistributeEventCommand GivenPaymentEventTrackingNumberIs(ProcessDistributeEventCommand command, string paymentEventTrackingNumber)
        {
            command.PaymentEvent.EventTrackingNumber = paymentEventTrackingNumber;
            return command;
        }

        private ProcessDistributeEventCommand GivenPaymentTypeIs(ProcessDistributeEventCommand command, string paymentType)
        {
            command.PaymentEvent.EventPaymentType = paymentType;
            return command;
        }

        private ProcessDistributeEventCommand GivenThePaymentStatusIsSet(ProcessDistributeEventCommand command, GuidewirePaymentStatus? status)
        {
            return new ProcessDistributeEventCommand(command.PaymentEvent, command.Settings, command.ClientCode, command.ClientPaymentId, status?.ToString() ?? "");
        }
    }
}
