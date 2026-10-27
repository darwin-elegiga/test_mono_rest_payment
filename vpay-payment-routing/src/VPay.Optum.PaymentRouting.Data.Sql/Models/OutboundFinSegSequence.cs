#nullable enable

using System;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public record OutboundFinSegSequence
    {
        public long OutboundFinSegSequenceId { get; init; } = default!;
        public string PayerAlias { get; init; } = default!;
        public string DateStamp { get; init; } = default!;
        public int CurrentSequenceNumber { get; init; }
        public string? CreatedBy { get; init; } = default!;
        public DateTime CreatedDateTime { get; init; } = default!;
        public string? LastModifiedBy { get; init; } = default!;
        public DateTime? LastModifiedDateTime { get; init; } = default!;
 
    }
}
