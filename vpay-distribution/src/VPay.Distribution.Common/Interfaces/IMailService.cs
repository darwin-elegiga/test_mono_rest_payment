using System.Threading;
using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// This service is used for mail processing
    /// </summary>
    public interface IMailService
    {
        /// <summary>
        /// Distribute all the mail transactions
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DistributeMail(DocDistMsg message, CancellationToken cancellationToken);
    }
}
