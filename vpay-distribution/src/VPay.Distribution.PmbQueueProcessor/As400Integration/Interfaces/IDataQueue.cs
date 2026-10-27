namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

public interface IDataQueue
{
    void Write(byte[] data);
}
