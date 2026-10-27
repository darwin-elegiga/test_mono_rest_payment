using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public interface IDocsExtractor
    {
        /// <summary>
        /// This method is used to perform the .docs file extraction. The implementation is handled in the individual parent classes which are created from the <see cref="IDocsExtractorFactory"/>
        /// </summary>
        /// <param name="tarDataFilename"></param>
        /// <param name="inputFilename"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        IEnumerable<Edi835ParsedDocsContent> ExtractDocs(string tarDataFilename, string inputFilename, List<DocumentTransactionMessage> messages);
    }
}
