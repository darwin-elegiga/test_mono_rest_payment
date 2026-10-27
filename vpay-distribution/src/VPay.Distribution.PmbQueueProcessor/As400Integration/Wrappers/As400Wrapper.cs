using com.ibm.as400.access;
using VPay.Distribution.PmbQueueProcessor.As400Integration.Interfaces;

namespace VPay.Distribution.PmbQueueProcessor.As400Integration.Wrappers
{
    public class As400Wrapper : IAs400
    {
        internal AS400 UnderlyingObject { get; }

        public As400Wrapper(AS400 as400)
        {
            UnderlyingObject = as400 ?? throw new ArgumentNullException(nameof(as400));
        }

        public int getCcsid()
        {
            return UnderlyingObject.getCcsid();
        }
    }
}
