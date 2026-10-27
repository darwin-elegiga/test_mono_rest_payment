using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public class DistributionMessage
    {
        public string StatusFileName { get; set; }

        public StatusFile StatusFile { get; set; }

        public long StatusFileId { get; set; }
    }
}
