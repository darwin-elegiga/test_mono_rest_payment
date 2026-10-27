using VPay.MassTransit.Abstractions;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Configurations
{
    public class OptumSettings : TransportConfig
    {
        public string EndpointName { get; set; } = string.Empty;
    }
}
