using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public interface ICoverVPaygeGeneratorService
    {
        /// <summary>
        /// Generate Cover VPayges
        /// </summary>
        /// <param name="documents"><see cref="IEnumerable{VPaygeDocumentType}"/> document types to create</param>
        /// <param name="param">parameters used to create the documents</param>
        /// <returns><see cref="IEnumerable{VPaygeGeneratedFile}"/></returns>
        Task<IEnumerable<VPaygeGeneratedFile>> GenerateCoverVPayges(IEnumerable<VPaygeDocumentType> documents, DocGenParamInternal param);
    }
}
