using System;

namespace VPay.DocSys.Generation.Common.Exceptions
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }

        public ApiException(int statusCode) : base()
        {
            StatusCode = statusCode;
        }

        public ApiException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public ApiException(int statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
