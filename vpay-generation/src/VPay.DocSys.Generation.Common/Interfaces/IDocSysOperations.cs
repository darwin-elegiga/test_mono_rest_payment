using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is the operations needed to generate documents for VPay
    /// </summary>
    public interface IDocSysOperations
    {
        /// <summary>
        /// This will determine all the Documents that need to be generated for the Transaction details
        /// </summary>
        /// <param name="docGenParam">This is the required information needed to determine the documents that need to be generated</param>
        /// <returns>The manifest of documents that need to be generated for a transaction</returns>
        Task<DocGenManifest> GetDocumentManifest(DocGenParam docGenParam);

        /// <summary>
        /// This will generate the EOB document for a Transaction
        /// </summary>
        /// <param name="docGenParam">This is the required information needed to generate the EOB document for a transaction</param>
        /// <returns>The eob pdf and xml that got generated for the transaction</returns>
        Task<IEnumerable<EobGeneratedFile>> GenerateEobDoc(DocGenParam docGenParam);

        /// <summary>
        /// This will generate all the VPayge Cover Sheet documents for a Transaction
        /// </summary>
        /// <param name="docGenParam">This is the required information needed to generate the VPayge Cover Sheet documents for a transaction</param>
        /// <returns>The list of VPayge Cover Sheet documents generated for a transaction</returns>
        Task<IEnumerable<VPaygeGeneratedFile>> GenerateVPaygeDocs(DocGenParam docGenParam);

        /// <summary>
        /// This will generate all documents needed for a Transaction
        /// </summary>
        /// <param name="docGenParam">This is the required information needed to generate all the documents for a transaction</param>
        /// <returns>The list of all documents (i.e. EOB, VPayge Cover Sheets, and Packets) generated for a transaction</returns>
        Task<IEnumerable<IGeneratedFile>> GenerateAllDocs(DocGenParam docGenParam);
    }
}
