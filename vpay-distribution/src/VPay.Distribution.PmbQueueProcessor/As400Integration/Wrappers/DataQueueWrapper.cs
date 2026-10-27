using com.ibm.as400.access;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Wrappers
{
    public class DataQueueWrapper : IDataQueue
    {
        private readonly DataQueue _dataQueue;

        public DataQueueWrapper(DataQueue dataQueue)
        {
            _dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
        }

        public void Write(byte[] data)
        {
            _dataQueue.write(data);
        }
    }
}
