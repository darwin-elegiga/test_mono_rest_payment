using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{

    /// <summary>
    /// This is the object returned for the generate all documents api method
    /// </summary>
    public class GeneratedFileResult
    {

        /// <summary>
        /// This is the type of file generated
        /// </summary>
        public GeneratedFileResultType? GeneratedFileType { get; set; }

        /// <summary>
        /// This is the transaction id of the generated document
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// This is the name and path of the generated file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// This is the detail type of the document that needed to be generated
        /// </summary>
        public FileDetailType FileDetailType { get; set; }

        /// <summary>
        /// This is the description of the document that was generated
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// This is the VPaygeDocumentType if <see cref="GeneratedFileType"/> is set to <see cref="GeneratedFileResultType.VPayge" />
        /// </summary>
        public VPaygeDocumentType? VPaygeDocumentType { get; set; }

        /// <summary>
        /// This is the VPaygeDocumentType included in the packet if <see cref="GeneratedFileType"/> is set to <see cref="GeneratedFileResultType.Packet" />
        /// </summary>
        public VPaygeDocumentType? IncludedVPaygeDocumentType { get; set; }

        /// <summary>
        /// This is the Eob generated type if <see cref="GeneratedFileType"/> is set to <see cref="GeneratedFileResultType.EOB" />
        /// </summary>
        public EobGenerateType? EobGenerateType { get; set; }

    }
}
