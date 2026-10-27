using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    public class CompareMessage : IMessage
    {
        public CompareMessage()
        {
            MessageType = "CompareMessage";
        }

        public string MessageType { get; set; }

        public long ReportRequestId { get; set; }
        public long ArchiveFileId { get; set; }
        public DbSystemType SourceSystem { get; set; } = DbSystemType.SqlServer;
        public DbSystemType CompareToSystem { get; set; } = DbSystemType.MySql;

    }
}
