using Newtonsoft.Json;

namespace VPay.Gateway.Common.Documents.Models;

public class ArchiveDetail
{
    public string? FileDetailType { get; set; }

    public long? ArchiveDetailId { get; set; }

    public string? Description { get; set; }

    public long? TransactionId { get; set; }

    public string? FileName { get; set; }

    public byte[]? Data { get; set; }

    public long? FileSize { get; set; }

    public int? PageCount { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? CreateDateTime { get; set; }

    public string? CreateLogin { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? ModifyDateTime { get; set; }

    public string? ModifyLogin { get; set; }

    public long? ArchiveFileId { get; set; }
}
