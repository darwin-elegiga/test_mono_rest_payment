using MediatR;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;

namespace VPay.Gateway.Common.Guidewire;

public sealed record SendPaymentStatusChangeCommand(
    GuidewirePaymentStatus PaymentStatus,
    string PaymentId,
    string Message,
    GuidewireSettings Settings
) : IRequest<GuidewireResult>;

public class SendPaymentStatusChangeCommandHandler : IRequestHandler<SendPaymentStatusChangeCommand, GuidewireResult>
{
    private readonly IGuidewireServiceProxy _guidewireService;

    public SendPaymentStatusChangeCommandHandler(IGuidewireServiceProxy guidewireService) =>
        _guidewireService = guidewireService;

    public async Task<GuidewireResult> Handle(SendPaymentStatusChangeCommand request, CancellationToken cancellationToken)
    {
        var payDetail = new OptumPaymentUpdateDetails
        {
            PaymentID = request.PaymentId,
            Status = request.PaymentStatus switch
            {
                GuidewirePaymentStatus.Cleared => "cleared",
                GuidewirePaymentStatus.Issued => "issued",
                GuidewirePaymentStatus.Stop => "stop",
                GuidewirePaymentStatus.PendingStop => "pendingstop",
                GuidewirePaymentStatus.Void => "void",
                GuidewirePaymentStatus.PendingVoid => "pendingvoid",
                _ => throw new InvalidOperationException($"Unknown {nameof(GuidewirePaymentStatus)} Value '{request.PaymentId}'")
            },
            NoteDetails = new NoteDetails
            {
                Body = request.Message,
                PaymentID = request.PaymentId,
                Subject = request.Message,
                Topic = NoteTopicType.general,
                TopicSpecified = true
            }
        };

        return await _guidewireService.UpdatePaymentDetails(payDetail, request.Settings).ConfigureAwait(false);
    }
}
