using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

public class GetPaymentStatusTrackingResult
{
    public Payment? Payment { get; set; }

    public List<PaymentStatusTracking>? StatusTrackings { get; set; }
}
