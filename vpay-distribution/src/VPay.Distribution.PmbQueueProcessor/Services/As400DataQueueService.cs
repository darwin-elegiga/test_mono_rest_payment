using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;
using VPay.Distribution.PmbQueueProcessor.Settings;

namespace VPay.Distribution.PmbQueueProcessor.Services;

public class As400DataQueueService : IAs400DataQueueService
{
    private readonly IAs400ConnectionPool _as400ConnectionPool;
    private readonly IAs400ObjectFactory _as400ObjectFactory;
    private readonly As400Settings _settings;
    private readonly IAs400 _as400Template;
    private readonly string _dataQueuePath;

    public As400DataQueueService(IAs400ConnectionPool as400ConnectionPool, IAs400ObjectFactory as400ObjectFactory, As400Settings settings)
    {
        _as400ConnectionPool = as400ConnectionPool ?? throw new ArgumentNullException(nameof(as400ConnectionPool));
        _as400ObjectFactory = as400ObjectFactory ?? throw new ArgumentNullException(nameof(as400ObjectFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _as400Template = _as400ObjectFactory.CreateAs400(settings.SystemName, settings.UserId, settings.Password);
        _dataQueuePath = $"/QSYS.LIB/{settings.LibraryName}.LIB/{settings.DataQueueName}.DTAQ";
    }

    public void WriteToQueue(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));
        }

        IAs400? as400Connection = null;
        try
        {
            as400Connection = _as400ConnectionPool.GetDatqQueueConnection(_as400Template);

            IDataQueue dataQueue = _as400ObjectFactory.CreateDataQueue(as400Connection, _dataQueuePath);
            IAs400Text as400Text = _as400ObjectFactory.CreateAs400Text(message.Length, as400Connection);
            byte[] data = as400Text.ToBytes(message);
            dataQueue.Write(data);
        }
        finally
        {
            if (as400Connection != null)
            {
                _as400ConnectionPool.ReturnToConnectionPool(as400Connection);
            }
        }
    }
}
