using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common
{
    public interface IEdi835TarService
    {

        /// <summary>
        /// Takes a list of <see cref="DocumentTransactionMessage"/>, using the first, gets the
        /// sts file and extracts the .docs file.
        /// </summary>
        /// <param name="messages">List of <see cref="DocumentTransactionMessage"/> representing the individual payment rows in the status file.</param>
        /// <param name="cancellationToken">To allow for asynchronous cancellations.</param>
        /// <returns>List of Edi835 DTO objects from database, if any.</returns>
        Task<IList<Edi835>> ExtractDocs(List<DocumentTransactionMessage> messages, CancellationToken cancellationToken);
 
        ///
        /// <summary>
        /// Exposes the EnableEdi835Parsing flag from <seealso cref="Edi835ServiceSettings.EnableEdi835Parsing"/>,
        /// as opposed to generically passing the IConfiguration object and having to know the correct
        /// manipulation to pull it from there.
        /// </summary>
        bool EnableEdi835Parsing { get; }

        /// <summary>
        /// Used in testing and API calls to independently get the list of <see cref="DocumentTransactionMessage"/>.
        /// </summary>
        /// <param name="stsFilename"></param>
        /// <returns></returns>
        List<DocumentTransactionMessage> ParseSts(string stsFilename);

    }
}
