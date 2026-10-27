using MediatR;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;

namespace VPay.Gateway.Common.Guidewire;

public sealed record SendEnrollmentStatusChangeCommand(
    GuidewireEnrollmentStatus EnrollmentStatus,
    string MemberId,
    string PayeeName,
    string? Reason,
    GuidewireSettings Settings
) : IRequest<GuidewireResult>;

public class SendEnrollmentStatusChangeCommandHandler : IRequestHandler<SendEnrollmentStatusChangeCommand, GuidewireResult>
{
    private readonly IGuidewireServiceProxy _guidewireService;

    public SendEnrollmentStatusChangeCommandHandler(IGuidewireServiceProxy guidewireService) =>
        _guidewireService = guidewireService;

    public async Task<GuidewireResult> Handle(SendEnrollmentStatusChangeCommand request, CancellationToken cancellationToken)
    {
        var message = request.EnrollmentStatus switch
        {
            GuidewireEnrollmentStatus.InProgress => $"Enrollment is in progress for {request.PayeeName}",
            GuidewireEnrollmentStatus.Enrolled => $"Enrollment successful for {request.PayeeName}",
            GuidewireEnrollmentStatus.Failed => $"Enrollment failed for {request.PayeeName}",
            _ => throw new InvalidOperationException($"Unknown {nameof(GuidewireEnrollmentStatus)} Value '{request.EnrollmentStatus}'")
        };

        var status = request.EnrollmentStatus switch
        {
            GuidewireEnrollmentStatus.InProgress => "inprogress",
            GuidewireEnrollmentStatus.Enrolled => "enrolled",
            GuidewireEnrollmentStatus.Failed => "failed",
            _ => throw new InvalidOperationException($"Unknown {nameof(GuidewireEnrollmentStatus)} Value '{request.EnrollmentStatus}'")
        };

        var payDetail = new OptumEnrollmentUpdateDetails
        {
            PayeeID = request.MemberId,
            Status = status,
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? message
                : request.Reason
        };

        return await _guidewireService.UpdateEnrollmentDetails(payDetail, request.Settings).ConfigureAwait(false);
    }
}
