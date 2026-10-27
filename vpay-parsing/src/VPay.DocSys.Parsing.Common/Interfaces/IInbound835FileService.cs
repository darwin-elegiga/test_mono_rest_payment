using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Provides methods for processing inbound 835 files.
    /// </summary>
    public interface IInbound835FileService
    {
        /// <summary>
        /// Queues the inbound 835 file specified by <paramref name="filePath"/> for processing.
        /// </summary>
        /// <param name="filePath">The full path and file name of the inbound 835 file to queue for processing.</param>
        Task QueueFileForProcessing(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queues the inbound 835 file specified in <paramref name="message" /> for processing.
        /// </summary>
        /// <param name="message">An <see cref="Inbound835FileMessage"/> instance that contains information about the inbound file to be queued for processing.</param>
        Task QueueFileForProcessing(Inbound835FileMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes the inbound 835 file specified by <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The full path and file name of the inbound 835 file to process.</param>
        Task ProcessFile(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes the inbound 835 file specified in <paramref name="message" />.
        /// </summary>
        /// <param name="message">An <see cref="Inbound835FileMessage"/> instance that contains information about the inbound file to be processed.</param>
        Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the 835 document for the specified <paramref name="transactionId"/> to Availity.
        /// </summary>
        /// <param name="transactionId">The transaction id of the 835 document to send to Availity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task Send835ToAvaility(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the BRN document for the specified <paramref name="transactionId"/> to Availity.
        /// </summary>
        /// <param name="transactionId">The transaction id of the BRN document to send to Availity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task SendBrnToAvaility(long transactionId, CancellationToken cancellationToken = default);
    }
}
