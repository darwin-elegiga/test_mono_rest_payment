using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Models.Sql;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation
{
    /// <summary>
    /// Bundled Fax Service
    /// </summary>
    public interface IBundledFaxService
    {
        /// <summary>
        /// Create a new pdf and queue for the <see cref="ITiffQueueMessageProcessor"/>
        /// </summary>
        /// <remarks>
        /// Will use the <paramref name="input"/> to do the following:
        /// - Create a cover page 
        /// - Fetch and combine the cover page with the given <see cref="BundledFaxParams.DetailIds"/> into a new pdf
        /// - Archive and associate the new pdf with the TransactionIds
        /// - Fetch a new Correspondence Id
        /// - Queue a new <see cref="TiffQueueMessage"/>
        /// </remarks>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<bool> CreateBundledFax(BundledFaxParams input);

        /// <summary>
        /// Generate a PDF Fax cover page
        /// </summary>
        /// <param name="input">Information about cover page and documents to be faxed</param>
        /// <returns>Path to rendered Fax Cover page PDF</returns>
        Task<string> CreateCoverPage(BundledFaxParams input);

        /// <summary>
        /// Formulates the bundled fax message to queue
        /// Exposed for testing
        /// </summary>
        /// <param name="bFaxResult"></param>
        /// <param name="bFaxParams"></param>
        /// <param name="transactions"></param>
        /// <param name="archiveFile"></param>
        /// <param name="pdfName"></param>
        /// <returns></returns>
        TiffQueueMessage FormulateBundledFaxMessage(BundledFaxCorrespondenceIdResult bFaxResult, BundledFaxParams bFaxParams, List<long> transactions, IArchiveFile archiveFile);

        /// <summary>
        /// Retry all the failed bundled faxes for the given <paramref name="startDate"/>
        /// and optional <paramref name="bundledFaxId"/>
        /// </summary>
        /// <remarks>
        /// All failed faxes go through <see cref="IBundledFaxService.CreateBundledFax"/>
        /// </remarks>
        /// <param name="startDate"></param>
        /// <param name="bundledFaxId"></param>
        /// <returns></returns>
        Task<List<string>> RetryFailedBundledFaxesAsync(DateTime startDate, long? bundledFaxId);

        /// <summary>
        /// Fetch all the failed bundled faxes for the given <paramref name="startDate"/>
        /// </summary>
        /// <param name="startDate"></param>
        /// <returns></returns>
        Task<List<FailedBundledFax>> GetFailedBundledFaxesAsync(DateTime startDate);

        /// <summary>
        /// Generates a Correspondence Id
        /// Only for use with <see cref="IBundledFaxService"/>
        /// </summary>
        /// <remarks>
        /// Associates the new id with the given <paramref name="transactionIds"/>
        /// </remarks>
        /// <param name="transactionIds"></param>
        /// <param name="faxNumber"></param>
        /// <param name="documentUrl"></param>
        /// <param name="pageCount"></param>
        /// <returns></returns>
        Task<BundledFaxCorrespondenceIdResult> GenerateBundledFaxCorrespondenceId(List<long> transactionIds, string faxNumber, string documentUrl, int pageCount);
    }
}
