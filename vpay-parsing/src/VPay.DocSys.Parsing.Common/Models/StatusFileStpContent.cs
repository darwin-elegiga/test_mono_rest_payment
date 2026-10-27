using System;
using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class StatusFileStpContent
    {
        internal const string StpNetworkType = "STP";

        private readonly StatusFile _statusFile;

        public IEnumerable<StatusFileStpGroup> StpGroups { get; internal set; }

        public StatusFile StatusFile => _statusFile;

        public string FileName => _statusFile?.Header?.FileName ?? string.Empty;

        public static string NetworkType => StpNetworkType;

        public StatusFileStpContent(StatusFile statusFile)
        {
            _statusFile = statusFile ?? throw new ArgumentNullException(nameof(statusFile));

            StpGroups = [];

            if (statusFile.Records == null || statusFile.Records.Count == 0)
            {
                return;
            }

            var stpRecords = statusFile.Records
                .Where(r => r.NETWORKTYPE.Equals(StpNetworkType, StringComparison.InvariantCultureIgnoreCase))
                .GroupBy(r => new { r.NETWORKNAME, r.NETWORKENROLLMENTTYPE })
                .Select(g => new StatusFileStpGroup
                {
                    NetworkName = g.Key.NETWORKNAME,
                    EnrollType = g.Key.NETWORKENROLLMENTTYPE,
                    TransactionIds = g.Select(t => long.Parse(t.TXID))
                });

            if (stpRecords != null && stpRecords.Any())
            {
                StpGroups = stpRecords;
            }
        }
    }    
}
