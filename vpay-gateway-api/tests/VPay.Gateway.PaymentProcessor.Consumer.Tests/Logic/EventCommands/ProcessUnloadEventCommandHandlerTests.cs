using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Logic;
using VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;
using Xunit;

namespace VPay.Gateway.PaymentProcessor.Consumer.Tests.Logic.EventCommands;

/* NOTE: The tests below currently only test the "Do Nothing" and "Missing Configuration" scenarios
 *      The reason for this is that the other statuses will will most likely be refactored in the
 *      very near future, and are primarily just "stub" handlers for now.  Not tested (yet):
 *          GuidewireUnloadProcessType.Cleared
 *          GuidewireUnloadProcessType.UnloadSendback
*/

/// <summary>
/// Tests for the <see cref="ProcessUnloadEventCommandHandler"/>
/// </summary>
public class ProcessUnloadEventCommandHandlerTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
    private readonly ProcessUnloadEventCommandHandler _handler;

    protected ProcessUnloadEventCommandHandlerTests() =>
        _handler = new ProcessUnloadEventCommandHandler(_mediatorMock.Object, new NullLogger<ProcessUnloadEventCommandHandler>());

    public void Dispose() =>
        _mediatorMock.VerifyAll();

    public class Handle : ProcessUnloadEventCommandHandlerTests
    {
        private ProcessUnloadEventCommand DefaultCommand { get; } =
            new ProcessUnloadEventCommand(
                new PaymentEvent
                {
                    Id = 1,
                    EventReasonCode = "4000",
                    EventStatus = nameof(PaymentEvent.EventStatus),
                    CurrentStatus = nameof(PaymentEvent.CurrentStatus)
                },
                new GuidewireSettings(),
                nameof(ProcessUnloadEventCommand.ClientCode),
                nameof(ProcessUnloadEventCommand.ClientPaymentId),
                nameof(ProcessUnloadEventCommand.PaymentStatus));

        [Fact]
        public async Task WhenGuidewireUnloadProcessNotFound_SendsPaymentStatusFailedAndReturnsNonSuccess()
        {
            _mediatorMock
                .Setup(x => x.Send(It.Is<GuidewireUnloadProcessQuery>(x => x.ReasonCode == DefaultCommand.PaymentEvent.EventReasonCode), CancellationToken.None))
                .ReturnsAsync((GuidewireUnloadProcess)null)
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(e =>
                        e.PaymentEventId == DefaultCommand.PaymentEvent.Id
                        && e.Status == PaymentNotificationStatus.Failed.Name
                        && e.PreviousStatus == DefaultCommand.PaymentEvent.CurrentStatus),
                    CancellationToken.None))
                .ReturnsAsync((PaymentEvent)null)
                .Verifiable();

            var result = await _handler.Handle(DefaultCommand, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeFalse();
                result.GuidewireStatus.Should().Be(DefaultCommand.PaymentStatus);
            }
        }

        [Fact]
        public async Task WhenGuidewireUnloadProcessIsDoNothing_SendsPaymentStatusSkippedAndReturnsSuccess()
        {
            _mediatorMock
                .Setup(x => x.Send(It.Is<GuidewireUnloadProcessQuery>(x => x.ReasonCode == DefaultCommand.PaymentEvent.EventReasonCode), CancellationToken.None))
                .ReturnsAsync(new GuidewireUnloadProcess(1, DefaultCommand.PaymentEvent.EventReasonCode, GuidewireUnloadProcessType.DoNothing, "Some comment"))
                .Verifiable();

            _mediatorMock
                .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(e =>
                        e.PaymentEventId == DefaultCommand.PaymentEvent.Id
                        && e.Status == PaymentNotificationStatus.Skipped.Name
                        && e.PreviousStatus == DefaultCommand.PaymentEvent.CurrentStatus),
                    CancellationToken.None))
                .ReturnsAsync((PaymentEvent)null)
                .Verifiable();

            var result = await _handler.Handle(DefaultCommand, CancellationToken.None).ConfigureAwait(false);

            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.GuidewireStatus.Should().Be(DefaultCommand.PaymentStatus);
            }
        }

        public class HandleUnloadReissue : Handle
        {
            private readonly string _comment = "Some Comment";

            public HandleUnloadReissue()
            {
                _mediatorMock
                    .Setup(x => x.Send(It.Is<GuidewireUnloadProcessQuery>(x => x.ReasonCode == DefaultCommand.PaymentEvent.EventReasonCode), CancellationToken.None))
                    .ReturnsAsync(new GuidewireUnloadProcess(1, DefaultCommand.PaymentEvent.EventReasonCode, GuidewireUnloadProcessType.UnloadReissue, _comment))
                    .Verifiable();
            }

            [Theory]
            [InlineData("STOPPAY")]
            [InlineData("STOPPAY_R")]
            [InlineData("UNLOADS")]
            [InlineData("UNLOADSF")]
            public async Task WhenGuidewireGetsUnloadStatusNotUNLOAD_SendsPaymentStatusSkippedAndReturnsSuccess(string unloadStatusCode)
            {
                DefaultCommand.PaymentEvent.EventStatus = unloadStatusCode;

                _mediatorMock
                    .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(e =>
                            e.PaymentEventId == DefaultCommand.PaymentEvent.Id
                            && e.Status == PaymentNotificationStatus.Skipped.Name
                            && e.PreviousStatus == DefaultCommand.PaymentEvent.CurrentStatus),
                        CancellationToken.None))
                    .ReturnsAsync((PaymentEvent)null)
                    .Verifiable();

                var result = await _handler.Handle(DefaultCommand, CancellationToken.None).ConfigureAwait(false);

                using (new AssertionScope())
                {
                    result.IsSuccess.Should().BeTrue();
                    result.GuidewireStatus.Should().Be(DefaultCommand.PaymentStatus);
                }
            }

            [Fact]
            public async Task WhenGuidewireGetsUnloadStatusOfUNLOAD_SendsNoteToGuidewireAndIsSuccessful_ThenSendsPaymentStatusSkippedAndReturnsSuccess()
            {
                var paymentUpdateResult = new GuidewireResult { Status = "SUCCESS" };
                var expectedPaymentStatusCodeCommand = new SendPaymentNoteCommand(DefaultCommand.ClientPaymentId, $"UNLOAD {DefaultCommand.PaymentEvent.EventReasonCode} - {_comment}", DefaultCommand.Settings);

                DefaultCommand.PaymentEvent.EventStatus = "UNLOAD";

                SendPaymentNoteCommand actPaymentStatusCodeCommand = null;
                _mediatorMock.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                    .ReturnsAsync(paymentUpdateResult)
                    .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                    {
                        actPaymentStatusCodeCommand = (SendPaymentNoteCommand)q;
                    })
                    .Verifiable();

                _mediatorMock
                    .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(e =>
                            e.PaymentEventId == DefaultCommand.PaymentEvent.Id
                            && e.Status == PaymentNotificationStatus.Completed.Name
                            && e.PreviousStatus == DefaultCommand.PaymentEvent.CurrentStatus),
                        CancellationToken.None))
                    .ReturnsAsync((PaymentEvent)null)
                    .Verifiable();

                var result = await _handler.Handle(DefaultCommand, CancellationToken.None).ConfigureAwait(false);

                using (new AssertionScope())
                {
                    result.IsSuccess.Should().BeTrue();
                    result.GuidewireStatus.Should().Be(DefaultCommand.PaymentStatus);
                    actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                }
            }

            [Fact]
            public async Task WhenGuidewireGetsUnloadStatusOfUNLOAD_SendsNoteToGuidewireAndNotSuccessful_ThenSendsPaymentStatusFailedAndReturnsFailed()
            {
                var paymentUpdateResult = new GuidewireResult { Status = "ERROR" };
                var expectedPaymentStatusCodeCommand = new SendPaymentNoteCommand(DefaultCommand.ClientPaymentId, $"UNLOAD {DefaultCommand.PaymentEvent.EventReasonCode} - {_comment}", DefaultCommand.Settings);

                DefaultCommand.PaymentEvent.EventStatus = "UNLOAD";

                SendPaymentNoteCommand actPaymentStatusCodeCommand = null;
                _mediatorMock.Setup(x => x.Send(It.IsAny<SendPaymentNoteCommand>(), default))
                    .ReturnsAsync(paymentUpdateResult)
                    .Callback<IRequest<GuidewireResult>, CancellationToken>((q, _) =>
                    {
                        actPaymentStatusCodeCommand = (SendPaymentNoteCommand)q;
                    })
                    .Verifiable();

                _mediatorMock
                    .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(e =>
                            e.PaymentEventId == DefaultCommand.PaymentEvent.Id
                            && e.Status == PaymentNotificationStatus.Failed.Name
                            && e.PreviousStatus == DefaultCommand.PaymentEvent.CurrentStatus),
                        CancellationToken.None))
                    .ReturnsAsync((PaymentEvent)null)
                    .Verifiable();

                var result = await _handler.Handle(DefaultCommand, CancellationToken.None).ConfigureAwait(false);

                using (new AssertionScope())
                {
                    result.IsSuccess.Should().BeFalse();
                    result.GuidewireStatus.Should().Be(DefaultCommand.PaymentStatus);
                    actPaymentStatusCodeCommand.Should().BeEquivalentTo(expectedPaymentStatusCodeCommand);
                }
            }
        }

        public class HandleUnloadSendback : Handle
        {
            private const string Comment = "Some Comment";

            public HandleUnloadSendback()
            {
                DefaultCommand.PaymentEvent.EventPaymentType = "CLVCP";
                _mediatorMock
                    .Setup(x => x.Send(It.Is<GuidewireUnloadProcessQuery>(q => q.ReasonCode == DefaultCommand.PaymentEvent.EventReasonCode), CancellationToken.None))
                    .ReturnsAsync(new GuidewireUnloadProcess(1, DefaultCommand.PaymentEvent.EventReasonCode, GuidewireUnloadProcessType.UnloadSendback, Comment))
                    .Verifiable();
            }

            [Fact]
            public async Task WhenPaymentTypeIsNotCard_ShouldUpdateStatusToFailed()
            {
                DefaultCommand.PaymentEvent.EventPaymentType = "NOTCARD";
                await VerifyPaymentEventStatusUpdate(PaymentNotificationStatus.Failed, false).ConfigureAwait(false);
            }

            [Theory]
            [InlineData("CLVCP", "UNLOAD", GuidewirePaymentStatus.Void, "UNLOAD {0} - {1} - Confirmed")]
            [InlineData("CLVCP", "UNLOADS", GuidewirePaymentStatus.PendingVoid, "UNLOAD {0} - {1} - Requested")]
            [InlineData("CLVCP", "UNLOADSF", GuidewirePaymentStatus.PendingVoid, "UNLOAD {0} - {1} - Requested")]
            [InlineData("CLCHK", "UNLOAD", GuidewirePaymentStatus.Stop, "STOPPAY {0} - {1} - Confirmed")]
            [InlineData("CLCHK", "STOPPAY", GuidewirePaymentStatus.PendingStop, "STOPPAY {0} - {1} - Requested")]
            public async Task WhenStatusSuccessfullySendsToGuidewire_ThenReturnsSuccess(
                string paymentType,
                string eventStatus,
                GuidewirePaymentStatus expectedStatus,
                string messageTemplate)
            {
                DefaultCommand.PaymentEvent.EventPaymentType = paymentType;
                DefaultCommand.PaymentEvent.EventStatus = eventStatus;

                _mediatorMock.Setup(x => x.Send(
                        It.Is<SendPaymentStatusChangeCommand>(c =>
                            c.PaymentStatus == expectedStatus
                            && c.PaymentId == DefaultCommand.ClientPaymentId
                            && c.Message == string.Format(messageTemplate, DefaultCommand.PaymentEvent.EventReasonCode, Comment)
                            && c.Settings == DefaultCommand.Settings),
                        CancellationToken.None))
                    .ReturnsAsync(new GuidewireResult
                    {
                        Status = "SUCCESS"
                    })
                    .Verifiable();

                await VerifyPaymentEventStatusUpdate(PaymentNotificationStatus.Completed, true).ConfigureAwait(false);
            }

            [Theory]
            [InlineData("CLVCP", "UNLOAD", GuidewirePaymentStatus.Void, "UNLOAD {0} - {1} - Confirmed")]
            [InlineData("CLVCP", "UNLOADS", GuidewirePaymentStatus.PendingVoid, "UNLOAD {0} - {1} - Requested")]
            [InlineData("CLVCP", "UNLOADSF", GuidewirePaymentStatus.PendingVoid, "UNLOAD {0} - {1} - Requested")]
            [InlineData("CLCHK", "UNLOAD", GuidewirePaymentStatus.Stop, "STOPPAY {0} - {1} - Confirmed")]
            [InlineData("CLCHK", "STOPPAY", GuidewirePaymentStatus.PendingStop, "STOPPAY {0} - {1} - Requested")]
            public async Task WhenStatusFailsToSendToGuidewire_ThenReturnsFailure(
                string paymentType,
                string eventStatus,
                GuidewirePaymentStatus expectedStatus,
                string messageTemplate)
            {
                DefaultCommand.PaymentEvent.EventPaymentType = paymentType;
                DefaultCommand.PaymentEvent.EventStatus = eventStatus;

                _mediatorMock.Setup(x => x.Send(
                        It.Is<SendPaymentStatusChangeCommand>(c =>
                            c.PaymentStatus == expectedStatus
                            && c.PaymentId == DefaultCommand.ClientPaymentId
                            && c.Message == string.Format(messageTemplate, DefaultCommand.PaymentEvent.EventReasonCode, Comment)
                            && c.Settings == DefaultCommand.Settings),
                        CancellationToken.None))
                    .ReturnsAsync(new GuidewireResult
                    {
                        Status = "ERROR"
                    })
                    .Verifiable();

                await VerifyPaymentEventStatusUpdate(PaymentNotificationStatus.Failed, false).ConfigureAwait(false);
            }

            [Theory]
            [InlineData("CLCHK", "STOPPAY_R")]
            [InlineData("CLCHK", "UNLOADS")]
            [InlineData("CLCHK", "UNLOADSF")]
            public async Task WhenStatusShouldBeSkipped_UpdatesToSkipped(string paymentType, string eventStatus)
            {
                DefaultCommand.PaymentEvent.EventPaymentType = paymentType;
                DefaultCommand.PaymentEvent.EventStatus = eventStatus;

                await VerifyPaymentEventStatusUpdate(PaymentNotificationStatus.Skipped, true).ConfigureAwait(false);
            }

            private async Task VerifyPaymentEventStatusUpdate(PaymentNotificationStatus expectedStatus, bool expectedProcessingResult)
            {
                _mediatorMock
                    .Setup(x => x.Send(It.Is<PaymentEventUpdateStatusCommand>(e =>
                            e.PaymentEventId == DefaultCommand.PaymentEvent.Id
                            && e.Status == expectedStatus.Name
                            && e.PreviousStatus == DefaultCommand.PaymentEvent.CurrentStatus),
                        CancellationToken.None))
                    .ReturnsAsync((PaymentEvent)null)
                    .Verifiable();

                var result = await _handler.Handle(DefaultCommand, CancellationToken.None).ConfigureAwait(false);

                using (new AssertionScope())
                {
                    result.IsSuccess.Should().Be(expectedProcessingResult);
                    result.GuidewireStatus.Should().Be(DefaultCommand.PaymentStatus);
                }
            }
        }
    }
}
