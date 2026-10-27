namespace VPay.Distribution.PmbQueueProcessor.Services;

public interface IAs400DataQueueService
{
    void WriteToQueue(string message);
}
