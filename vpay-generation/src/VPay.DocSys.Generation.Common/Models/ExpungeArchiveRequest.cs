using System;

namespace VPay.DocSys.Generation.Common
{
    public class ExpungeArchiveRequest
    {
        public Guid TransactionGuid { get; set; }
        public long ArchiveId { get; set; }
    }
}
