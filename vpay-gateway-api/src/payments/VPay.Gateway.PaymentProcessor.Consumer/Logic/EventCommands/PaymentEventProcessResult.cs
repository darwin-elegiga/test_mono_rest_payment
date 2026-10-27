namespace VPay.Gateway.PaymentProcessor.Consumer.Logic.EventCommands;

public class PaymentEventProcessResult
{
    public PaymentEventProcessResult(bool isSuccess, string guidewireStatus)
    {
        IsSuccess = isSuccess;
        GuidewireStatus = guidewireStatus;
    }

    public bool IsSuccess { get; }

    public string GuidewireStatus { get; set; }
}
