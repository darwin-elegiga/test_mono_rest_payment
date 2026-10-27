using MediatR;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;

namespace VPay.Gateway.Common.Guidewire;

public sealed record SendPaymentNoteCommand(
    string PaymentId,
    string Message,
    GuidewireSettings Settings
) : IRequest<GuidewireResult>;

public class SendPaymentNoteCommandHandler : IRequestHandler<SendPaymentNoteCommand, GuidewireResult>
{
    private readonly IGuidewireServiceProxy _guidewireService;

    public SendPaymentNoteCommandHandler(IGuidewireServiceProxy guidewireService) =>
        _guidewireService = guidewireService;

    public async Task<GuidewireResult> Handle(SendPaymentNoteCommand request, CancellationToken cancellationToken)
    {
        var note = new NoteDetails
        {
            Body = request.Message,
            PaymentID = request.PaymentId,
            Subject = request.Message,
            Topic = NoteTopicType.general,
            TopicSpecified = true
        };

        return await _guidewireService.CreateNote(note, request.Settings).ConfigureAwait(false);
    }
}
