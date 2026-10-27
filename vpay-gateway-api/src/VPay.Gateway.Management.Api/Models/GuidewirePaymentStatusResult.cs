namespace VPay.Gateway.Management.Api.Models;

public class GuidewirePaymentStatusResult
{
    public string ClientCode { get; set; }

    public string ClientPaymentId { get; set; }

    public string CurrentStatus { get; set; }

    public List<GuidewirePaymentStatusTrackingResult> Statuses { get; set; }
}
