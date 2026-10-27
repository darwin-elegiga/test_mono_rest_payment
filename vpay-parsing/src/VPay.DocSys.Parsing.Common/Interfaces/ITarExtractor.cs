using System.IO;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{
    public interface ITarExtractor
    {
        /// <summary>
        /// Opens the tar data file, attempts to locate the given entry, and return as a memory stream.
        /// </summary>
        /// <param name="tarFilePath"></param>
        /// <param name="entryFilename"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        MemoryStream StreamTarEntry(string tarFilePath, string entryFilename, Encoding encoding);
    }
}
