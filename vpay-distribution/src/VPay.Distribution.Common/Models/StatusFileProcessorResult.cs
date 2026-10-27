using System.Collections.Generic;

namespace VPay.Distribution.Common
{
    public class StatusFileProcessorResult
    {
        public IList<StatusRecordGenerated> FaxRecords { get; } = new List<StatusRecordGenerated>();

        public IList<StatusRecordGenerated> PrintRecords { get; } = new List<StatusRecordGenerated>();

        public IList<StatusRecordGenerated> UnloadRecords { get; } = new List<StatusRecordGenerated>();
    }
}
