using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing.Common.Interfaces;

/// <summary>
/// Provides methods for payment engine confirmation files.
/// </summary>
public interface IPaymentEngineConfirmationFileService
{
    /// <summary>
    /// Processes the inbound 835 file specified by <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">The full path and file name of the payment engine confirmation file to process.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    Task ProcessFile(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes the inbound 835 file specified in <paramref name="message" />.
    /// </summary>
    /// <param name="message">An <see cref="PaymentEngineConfirmationMessage"/> instance that contains information about the payment engine confirmation file to be processed.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    Task ProcessFile(PaymentEngineConfirmationMessage message, CancellationToken cancellationToken = default);
}
