namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

public interface IAs400ObjectFactory
{
    IAs400 CreateAs400(string systemName, string userId, string password);

    IDataQueue CreateDataQueue(IAs400 connection, string path);

    IAs400Text CreateAs400Text(int length, IAs400 connection);
}
