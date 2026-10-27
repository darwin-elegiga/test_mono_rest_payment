using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;

namespace VPay.Gateway.Common.Guidewire;

public interface IGuidewireServiceProxy
{
    Task<GuidewireResult> UpdatePaymentDetails(OptumPaymentUpdateDetails details, GuidewireSettings settings);

    Task<GuidewireResult> NotifyDocumentDetails(OptumNotifyDocumentDetails details, GuidewireSettings settings);

    Task<GuidewireResult> UpdateEnrollmentDetails(OptumEnrollmentUpdateDetails details, GuidewireSettings settings);

    Task<GuidewireResult> CreateNote(NoteDetails details, GuidewireSettings settings);
}
