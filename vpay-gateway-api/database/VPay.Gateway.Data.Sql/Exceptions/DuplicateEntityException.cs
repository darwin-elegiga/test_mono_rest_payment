namespace VPay.Gateway.Data.Sql.Exceptions;

public class DuplicateEntityException : Exception
{
    public DuplicateEntityException()
    {
    }

    public DuplicateEntityException(string message) : base(message)
    {
    }

    public DuplicateEntityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
