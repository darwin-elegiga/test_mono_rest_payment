using VPay.MassTransit.Abstractions;

namespace VPay.Optum.PaymentRouting.Api.Configuration
{
    public class OptumSettings : TransportConfig
    {
        public string EndpointName { get; set; } = string.Empty;
    }
}
