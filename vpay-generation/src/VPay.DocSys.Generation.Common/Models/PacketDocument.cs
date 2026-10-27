using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class PacketDocument
    {
        public List<string> CustomPdfs { get; set; }

        public List<PacketIncludedDocumentType> DocumentsNeeded { get; set; } = new List<PacketIncludedDocumentType>();

        public bool ForceEobToEvenPages { get; set; } = false;

        public VPaygeDocumentType DocumentType { get; set; }

    }
}
