using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VPay.DocSys.Generation.Common
{
    public interface IWarrantyPdfCreator
    {
        /// <summary>
        /// Creates a pdf from the xsl url path and given xml
        /// </summary>
        /// <param name="xslUrlPath">url path of xsl file</param>
        /// <param name="xNode">xml as <see cref="XDocument"/> XNode</param>
        /// <returns><see cref="MemoryStream"/> of pdf bytes</returns>
        Task<MemoryStream> CreatePdfStream(string xslUrlPath, XNode xNode);
    }
}
