namespace VPay.Gateway.Common.Documents.Models;

public class Document
{
    public Document(Stream stream, string contentType, string fileName)
    {
        Stream = stream;
        ContentType = contentType;
        FileName = fileName;
    }

    public Stream Stream { get; }

    public string ContentType { get; }

    public string FileName { get; }
}
