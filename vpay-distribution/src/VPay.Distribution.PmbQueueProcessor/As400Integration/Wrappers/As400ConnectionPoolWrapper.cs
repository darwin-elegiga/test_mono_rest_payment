using com.ibm.as400.access;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Wrappers
{
    public class As400ConnectionPoolWrapper : IAs400ConnectionPool
    {
        private readonly AS400ConnectionPool _as400ConnectionPool;

        public As400ConnectionPoolWrapper(AS400ConnectionPool as400ConnectionPool)
        {
            _as400ConnectionPool = as400ConnectionPool ?? throw new ArgumentNullException(nameof(as400ConnectionPool));
        }

        public IAs400 GetConnection(IAs400 systemTemplate, int service)
        {
            var as400Wrapper = (As400Wrapper)systemTemplate;
            AS400 connection = _as400ConnectionPool.getConnection(as400Wrapper.UnderlyingObject, service);
            return new As400Wrapper(connection);
        }

        public IAs400 GetDatqQueueConnection(IAs400 systemTemplate)
        {
            return GetConnection(systemTemplate, AS400.DATAQUEUE);
        }

        public void ReturnToConnectionPool(IAs400 connection)
        {
            var as400Wrapper = (As400Wrapper)connection;
            _as400ConnectionPool.returnConnectionToPool(as400Wrapper.UnderlyingObject);
        }
    }
}
