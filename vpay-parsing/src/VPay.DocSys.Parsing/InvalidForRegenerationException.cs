using System;
using System.Runtime.Serialization;

namespace VPay.DocSys.Parsing
{
    [Serializable]
    public class InvalidForRegenerationException : Exception
    {
        public InvalidForRegenerationException()
        {
        }

        public InvalidForRegenerationException( string message ) : base( message )
        {
        }

        public InvalidForRegenerationException( string message, Exception innerException ) : base( message, innerException )
        {
        }

        protected InvalidForRegenerationException( SerializationInfo info, StreamingContext context ) : base( info, context )
        {
        }
    }
}
