using VPay.DocSys.Common;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    public class PacketGeneratedFile : IGeneratedFile
    {

        public string TransactionId { get; set; }

        public string FileName { get; set; }

        public FileDetailType FileDetailType { get; set; }

        public string Description { get; set; }
        public int PageCount { get; set; }

        public VPaygeDocumentType IncludedVPaygeDocumentType { get; set; }

    }
}
