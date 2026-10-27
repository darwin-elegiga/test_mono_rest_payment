namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class NotifyClientPaymentChanged
{
    public string ClientCode { get; set; }

    public string PaymentId { get; set; }

    public DateTime RequestTime { get; set; }
}
