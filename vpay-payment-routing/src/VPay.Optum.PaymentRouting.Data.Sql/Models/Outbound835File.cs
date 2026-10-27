#nullable enable

using System;
using System.Collections.Generic;
using VPay.Optum.PaymentRouting.Data.SmartEnums;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public record Outbound835File
    {
        public long Id { get; init; } = default!;
        public string PayerAlias { get; init; } = default!;
        public string? OutboundRefEO { get; init; } = default!;
        public Outbound835FileStatus Status { get; init; } = default!;
        public Guid? QueueMessageId { get; init; }
        public string? FilePath { get; init; }
        public string CreatedBy { get; init; } = default!;
        public DateTime CreatedDateTime { get; init; } = default!;
        public string LastModifiedBy { get; init; } = default!;
        public DateTime LastModifiedDateTime { get; init; } = default!;
        public DateTime? SentDateTime { get; init; }
        public int? SequenceNumber { get; set; }
        public string? DateStamp { get; set; }
        public List<long> PaymentIds { get; set; } = new();
    }
}
