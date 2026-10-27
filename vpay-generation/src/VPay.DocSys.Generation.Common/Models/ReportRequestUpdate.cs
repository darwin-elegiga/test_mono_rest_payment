using System;

namespace VPay.DocSys.Generation.Common
{
    public class ReportRequestUpdate
    {
        public ReportRequestUpdate()
        {
        }

        public ReportRequestUpdate(long reportRequestId )
        {
            ReportRequestId = reportRequestId;
        }
        public long ReportRequestId { get; set;  }
        public ReportRequestStatus ReportRequestStatus { get; set; } = ReportRequestStatus.Unknown;
        public long? TotalSourceTransactionsFound { get; set;  }
        public long? TotalTransactionsCompared { get; set; }
        public DateTime? ActualEndDateTime { get; set; }
        public Exception Exception { get; set; }



    }
}
