using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public interface ICustomSendBackDocumentGeneratorService
    {
        /// <summary>
        /// Generate Custom Send Back Documents
        /// </summary>
        /// <param name="param">parameters used to create the documents</param>
        /// <returns><see cref="IEnumerable{VPaygeGeneratedFile}"/></returns>
        Task<IEnumerable<CustomSendBackGeneratedFile>> GenerateDocuments(DocGenParamInternal param);
    }
}
