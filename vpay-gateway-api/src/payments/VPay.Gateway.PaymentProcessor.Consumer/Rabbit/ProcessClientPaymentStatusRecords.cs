namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class ProcessClientPaymentStatusRecords
{
    public string ClientCode { get; set; }

    public string PaymentId { get; set; }

    public DateTime RequestTime { get; set; }
}
