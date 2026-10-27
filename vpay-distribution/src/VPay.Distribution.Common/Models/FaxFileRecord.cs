using System;

namespace VPay.Distribution.Common
{
    public class FaxFileRecord
    {
        public string TransactionId { get; set; }

        public string PayType { get; set; }

        public string DistType { get; set; } = DocSys.Common.DistType.Fax;

        public string Status { get; set; } = "*";

        public string FilePath { get; set; }

        public Guid? NewGuid { get; set; }
    }
}
