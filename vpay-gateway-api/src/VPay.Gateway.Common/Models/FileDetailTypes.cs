using Ardalis.SmartEnum;

namespace VPay.Gateway.Common.Models;

public sealed class FileDetailType : SmartEnum<FileDetailType>
{
    public static readonly FileDetailType FaxDocument = new("Fax Document", 1);
    public static readonly FileDetailType Generic = new("Generic", 2);
    public static readonly FileDetailType Pdf = new("Pdf", 3);
    public static readonly FileDetailType Text = new("Text", 4);
    public static readonly FileDetailType Hidden = new("Hidden", 5);
    public static readonly FileDetailType Limited = new("Limited", 6);
    public static readonly FileDetailType ClearedCheck = new("Cleared Check", 7);

    public FileDetailType(string name, int value) : base(name, value)
    {
    }
}
