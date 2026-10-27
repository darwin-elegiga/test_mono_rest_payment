using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// Processes requests for Custom Send Back distribution
    /// </summary>
    public interface ICustomSendBackProcessor
    {
        /// <summary>
        /// Merges the check PDFs for each transaction in the <paramref name="distributionMessage"/> into a single PDF and writes it to the configured CustomSendBackPath.
        /// </summary>
        /// <param name="distributionMessage">The distribution message that contains the transactions to process.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="DistributionProcessResult"/> that contains information about the results of the operation.</returns>
        Task<DistributionProcessResult> Process(DocDistMsg distributionMessage, CancellationToken cancellationToken = default);
    }
}
