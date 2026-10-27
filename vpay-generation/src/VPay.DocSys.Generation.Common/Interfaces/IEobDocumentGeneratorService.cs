using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public interface IEobDocumentGeneratorService
    {
        /// <summary>
        /// Generate EOB Documents
        /// </summary>
        /// <param name="eobGenerateType">Type of eob document to generate</param>
        /// <param name="param">parameters used in document generation</param>
        /// <returns><see cref="IEnumerable{EobGeneratedFile}"/></returns>
        Task<IEnumerable<EobGeneratedFile>> GenerateEobDocuments(EobGenerateType eobGenerateType, DocGenParamInternal param);
    }
}
