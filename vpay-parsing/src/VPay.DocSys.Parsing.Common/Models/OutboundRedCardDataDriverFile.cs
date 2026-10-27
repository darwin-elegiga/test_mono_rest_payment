using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class OutboundRedCardDataDriverFile
    {
        public OutboundRedCardDataDriverFile(string[] lines)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(lines, nameof(lines));

            Records = lines
                .Select(l => new OutboundRedCardDataDriverRecord(l))
                .ToList();
        }

        public List<OutboundRedCardDataDriverRecord> Records { get; set; }
    }
}
