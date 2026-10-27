using System;
using System.Net;

namespace VPay.Optum.Http.ConfigApi;

public class ConfigApiEndpointException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public ConfigApiEndpointException(string message)
        : base(message)
    {
    }

    public ConfigApiEndpointException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public ConfigApiEndpointException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConfigApiEndpointException(string message, Exception innerException, HttpStatusCode statusCode)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
