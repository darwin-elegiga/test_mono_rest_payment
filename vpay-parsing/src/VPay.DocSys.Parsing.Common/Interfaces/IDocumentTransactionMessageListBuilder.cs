using System.Collections.Generic;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IDocumentTransactionMessageListBuilder
    {
        /// <summary>
        /// Will create <see cref="DocumentTransactionMessage"/>s for the <see cref="StatusRecord"/>s in <paramref name="statusFile"/>
        /// </summary>
        /// <remarks>
        /// Will also set
        /// - .xml check information
        /// - .sub file information
        /// - .otb data
        /// - missing distribution types
        /// </remarks>
        /// <param name="tpaConfig">TPA Configuration as <see cref="DocSysConfiguration"/></param>
        /// <param name="statusFile"><see cref="StatusFile"/> object</param>
        /// <param name="statusFilePath">status file path</param>
        /// <param name="transactionIds"><see cref="IList{T}"/> of transaction Ids </param>
        /// <param name="isRegenRequest">flag for Regen Request</param>
        /// <param name="isReIssue">flag for ReIssue</param>
        /// <param name="isCustomSendBackTpa">flag for Custom SendBack TPA</param>
        /// <param name="isRedCard">flag for RedCard TPA</param>
        /// <returns><see cref="List{T}"/> of <see cref="DocumentTransactionMessage"/> </returns>
        Task<List<DocumentTransactionMessage>> BuildMessagesAsync(
            DocSysConfiguration tpaConfig,
            StatusFile statusFile,
            string statusFilePath,
            IList<long> transactionIds,
            bool isRegenRequest,
            bool isReIssue,
            bool isCustomSendBackTpa,
            bool isRedCard);
    }
}
