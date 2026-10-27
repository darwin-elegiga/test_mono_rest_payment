using System.IO;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Fop Api Client
    /// </summary>
    public interface IFopApiClient
    {
        /// <summary>
        /// Will return a PDF file as a stream using the given fo-xsl <paramref name="xmlContent"/>
        /// </summary>
        /// <param name="xmlContent">fo-xsl that is used to create a PDF</param>
        /// <returns>PDF file as <see cref="Stream"/></returns>
        Task<Stream> PostFile(string xmlContent);
    }
}
