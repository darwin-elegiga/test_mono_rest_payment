namespace VPay.Distribution.Common.Interfaces
{
    public interface IPmbMessage
    {
        string CreateTimestamp { get; set; }
        string Data { get; set; }
        string Family { get; set; }
        string FromSystem { get; set; }        
        string MessageType { get; }

        string MessageId { get; }
        string FormatId { get; set; }
        string OriginAction { get; }      
        string Padding { get; }
        string Priority { get; set; }
        string ToSystem { get; set; }
        string Version { get; }
    }
}
