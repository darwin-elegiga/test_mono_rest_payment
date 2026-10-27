using System;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Provide methods used to process inbound 835 files coming from EDI vendor (SDS, Availity, etc).
    /// </summary>
    public interface IInbound835FileProcessor
    {
        /// <summary>
        /// Processes the inbound 835 file specified in <paramref name="message" />.
        /// </summary>
        /// <param name="message">An <see cref="Inbound835FileMessage" /> instance that contains information about the inbound file to be processed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default);
    }
}
