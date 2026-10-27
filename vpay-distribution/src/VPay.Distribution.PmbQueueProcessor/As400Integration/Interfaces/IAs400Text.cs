namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

public interface IAs400Text
{
    byte[] ToBytes(string data);
}
