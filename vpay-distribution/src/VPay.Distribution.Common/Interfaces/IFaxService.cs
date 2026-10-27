using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// This service is used for all fax processing
    /// </summary>
    public interface IFaxService
    {
        /// <summary>
        /// Distribute all fax transactions, covers the following scenarios:
        /// - Publish message to tiff queue for faxing (if it has a <see cref="DistTxnData.CorrespondenceId"/>), otherwise:
        /// - legacy .fax file distribution for redcard and non-redcard
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="DistributionProcessResult"/></returns>
        Task DistributeFaxes(DocDistMsg message, CancellationToken cancellationToken);
    }
}
