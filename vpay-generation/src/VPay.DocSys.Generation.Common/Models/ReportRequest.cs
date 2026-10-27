using System;

namespace VPay.DocSys.Generation.Common
{
    public class ReportRequest
    {
        public long ReportRequestId { get; set; }
        public ReportRequestStatus Status { get; set; }
        public bool IsComplete { get; set; }
        public string SourceSystem { get; set; }
        public string CompareToSystem { get; set; }
        public int TotalTransactionsCompared { get; set; }
        public int TotalSourceTransactionsFound { get; set; }
        public DateTime? RequestBeginDateTime { get; set; }
        public DateTime? RequestEndDateTime { get; set; }
        public DateTime? ActualBeginDateTime { get; set; }
        public DateTime? ActualEndDateTime { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime ModifyDateTime { get; set; }
        public string ExceptionMessage { get; set; }
        public string CreateUser { get; set; }
        public string ModifyUser { get; set; }

    }
}
