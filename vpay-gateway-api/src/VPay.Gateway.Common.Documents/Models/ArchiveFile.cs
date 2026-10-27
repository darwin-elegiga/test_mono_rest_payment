using Newtonsoft.Json;

namespace VPay.Gateway.Common.Documents.Models;

public class ArchiveFile
{
    public long? TransactionId { get; set; }

    public Guid? DocumentUniqueId { get; set; }

    public string? FileName { get; set; }

    public string? PathName { get; set; }

    public string? StageFileKey { get; set; }

    public string? Tpa { get; set; }

    public string? ReferenceCode { get; set; }

    public List<ArchiveDetail>? ArchiveDetails { get; set; }

    public string? DbServerName { get; set; }

    public string? FileServerName { get; set; }

    public bool? Visible { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? CreateDateTime { get; set; }

    public string? CreateLogin { get; set; }

    [JsonConverter(typeof(UTCDateTimeConverter))]
    public DateTime? ModifyDateTime { get; set; }

    public string? ModifyLogin { get; set; }

    public long? ArchiveFileId { get; set; }
}
