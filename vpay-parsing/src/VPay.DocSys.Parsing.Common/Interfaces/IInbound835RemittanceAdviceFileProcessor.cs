using System;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// An <see cref="IInbound835FileProcessor"/> that processes <see cref="Inbound835FileType.RemittanceAdvice"/> files.
    /// </summary>
    public interface IInbound835RemittanceAdviceFileProcessor : IInbound835FileProcessor
    {
        /// <summary>
        /// Transfers the 835 document for the specified <paramref name="transactionId"/> to the location to be picked up and sent to Availity.
        /// </summary>
        /// <param name="transactionId">The transaction id of the 835 document to find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task Send835ToAvaility(long transactionId, CancellationToken cancellationToken);

        /// <summary>
        /// Transfers the BRN document for the specified <paramref name="transactionId"/> to the location to be picked up and sent to Availity.
        /// </summary>
        /// <param name="transactionId">The transaction id of the BRN document to find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task SendBrnToAvaility(long transactionId, CancellationToken cancellationToken);
    }
}
