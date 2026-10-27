namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

public interface IAs400ConnectionPool
{
    IAs400 GetConnection(IAs400 systemTemplate, int service);

    IAs400 GetDatqQueueConnection(IAs400 systemTemplate);

    void ReturnToConnectionPool(IAs400 connection);
}
