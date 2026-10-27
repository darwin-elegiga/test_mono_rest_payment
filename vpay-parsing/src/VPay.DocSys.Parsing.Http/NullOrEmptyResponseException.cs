using System;
using System.Runtime.Serialization;

namespace VPay.DocSys.Parsing.Http
{
    [Serializable]
    public class NullOrEmptyResponseException : Exception
    {
        public NullOrEmptyResponseException()
            : base() { }
        protected NullOrEmptyResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        public NullOrEmptyResponseException(string message)
            : base(message) { }
        public NullOrEmptyResponseException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
