using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class ReissueEOBData
    {
        public DbSystemType ArchiveSource;
        public long ArchiveFileId;
        public string OriginalGuid;
        public string ReplacementGuid;
        public string OriginalTransactionId;
        public string ReissuedTransactionId;
        public int ReissueCount;
        public string InputFile;

        public string OriginalXmlFileName;
    }
}
