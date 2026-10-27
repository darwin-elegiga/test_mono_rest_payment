namespace VPay.Gateway.Common.Documents;

/// <summary>
/// Exception thrown when trying to access a document that is not
/// in the archive being accessed.
/// </summary>
public class DocumentNotInArchiveException : Exception
{
    public DocumentNotInArchiveException()
    {
    }

    public DocumentNotInArchiveException(string message) : base(message)
    {
    }

    public DocumentNotInArchiveException(string message, Exception inner) : base(message, inner)
    {
    }
}
