namespace VPay.Gateway.Common.Guidewire.Models;

public enum GuidewirePaymentStatus
{
    Requested,
    Issued,
    Cleared,
    PendingStop,
    Stop,
    PendingVoid,
    Void
}
