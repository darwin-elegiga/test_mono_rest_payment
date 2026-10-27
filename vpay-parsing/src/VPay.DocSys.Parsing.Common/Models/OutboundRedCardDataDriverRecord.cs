using System;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class OutboundRedCardDataDriverRecord
    {
        public OutboundRedCardDataDriverRecord(string line)
        {
            ArgumentHelpers.ThrowIf.IsNull(line, nameof(line));

            var split = line.Split('|').Select(s => s.Trim()).ToArray();

            if(split.Length < 8)
                throw new ArgumentException($"Invalid {nameof(OutboundRedCardDataDriverRecord)} record line.", nameof(line));

            this.Guid = Guid.Parse(split[0]);
            this.CoverFileName = split[1];
            this.MemberFileName = split[4];
            this.StatusFileName = split[6];
            this.TransactionID = long.Parse(split[8]);

            this.Status = OutboundRedCardDataDriverRecordStatus.Unknown;
        }

        public Guid Guid { get; set; }
        public string CoverFileName { get; set; }
        public string MemberFileName { get; set; }
        public string StatusFileName { get; set; }
        public long TransactionID { get; set; }
        public OutboundRedCardDataDriverRecordStatus Status { get; set; }
    }
}
