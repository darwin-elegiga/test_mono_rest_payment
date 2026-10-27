using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This contains the list of all documents that need to be generated for a transaction
    /// </summary>
    public class DocGenManifest
    {
        /// <summary>
        /// This is the EOB document type that need to for a Transaction
        /// </summary>
        public EobGenerateType EobGenerateType { get; set; } = EobGenerateType.None;

        /// <summary>
        /// This is the list of VPayge Cover Sheets that need to be generated for a transaction
        /// </summary>
        public List<VPaygeDocumentType> CoverSheetDocuments { get; set; } = new List<VPaygeDocumentType>();

        /// <summary>
        /// This is the list of Packets that need to be generated for a transaction
        /// </summary>
        /// <remarks>There should be one of these per <see cref="CoverSheetDocuments"/> . <see cref="EobGenerateType"/> and <see cref="CoverSheetDocuments"/> must be generated before generating these.</remarks>
        public List<PacketDocument> PacketDocuments { get; set; } = new List<PacketDocument>();
    }
}
