using System.IO;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Static File Api for the Client Asset Server
    /// </summary>
    public interface IStaticFileApiClient
    {
        /// <summary>
        /// Returns file content
        /// Throws an exception if file is not found or is not accessible
        /// </summary>
        /// <param name="filename">filePath</param>
        /// <returns>content as a <see cref="Stream"/></returns>
        Task<Stream> GetContentStream(string filename);
    }
}
