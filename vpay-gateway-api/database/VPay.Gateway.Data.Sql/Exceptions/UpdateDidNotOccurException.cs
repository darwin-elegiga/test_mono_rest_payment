using System.Runtime.Serialization;

namespace VPay.Gateway.Data.Sql.Exceptions;

[Serializable]
public class UpdateDidNotOccurException : Exception
{
    public UpdateDidNotOccurException()
    {
    }

    public UpdateDidNotOccurException(string message) : base(message)
    {
    }

    public UpdateDidNotOccurException(string message, Exception inner) : base(message, inner)
    {
    }

    protected UpdateDidNotOccurException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}
