using Newtonsoft.Json;
using VPay.Gateway.Common;

namespace VPay.Gateway.Api.Models;

public class FilteredDocumentDto
{
    public long? TransactionId { get; set; }

    public long? DocumentId { get; set; }

    public string FileName { get; set; }

    public string FileDetailType { get; set; }

    public string Description { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? CreateDateTime { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? ModifyDateTime { get; set; }
}
