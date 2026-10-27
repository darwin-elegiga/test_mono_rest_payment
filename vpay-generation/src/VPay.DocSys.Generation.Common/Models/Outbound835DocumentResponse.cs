using System.IO;

namespace VPay.DocSys.Generation.Common.Models;

public class Outbound835DocumentResponse
{
    public Stream Stream { get; set; }

    public string ContentType { get; set; }

    public string FileName { get; set; }
}
