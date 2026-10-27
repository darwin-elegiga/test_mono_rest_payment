namespace VPay.Gateway.Management.Api.Models;

public enum SendGuidewirePaymentStatus
{
    Issued,
    Cleared,
    PendingStop,
    Stop,
    PendingVoid,
    Void
}
