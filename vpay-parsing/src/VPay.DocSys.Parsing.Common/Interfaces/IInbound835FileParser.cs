using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Parsing.Common.Inbound835Files;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Provides methods for parsing inbound 835 files.
    /// </summary>
    public interface IInbound835FileParser
    {
        /// <summary>
        /// Gets the <see cref="Inbound835FileType"/> associated with the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>
        /// An <see cref="Inbound835FileType"/> value determined by the file name and extension of the specified <paramref name="path"/>.
        /// </returns>
        Inbound835FileType GetFileType(string path);

        /// <summary>
        /// Parses an inbound 835 acknowledgment file.
        /// </summary>
        /// <param name="path">The path to the file to parse.</param>
        /// <returns>An <see cref="AcknowledgmentFile"/> instance that represents the parsed file.</returns>
        Task<AcknowledgmentFile> ParseAcknowledgmentFile(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses an inbound 835 payment posted vendor response file.
        /// </summary>
        /// <param name="path">The path to the file to parse.</param>
        /// <returns>An <see cref="PaymentPostedFile"/> instance that represents the parsed file.</returns>
        Task<PaymentPostedFile> ParsePaymentPostedFile(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses an inbound 835 remittance advice file.
        /// </summary>
        /// <param name="path">The path to the file to parse.</param>
        /// <returns>An <see cref="RemittanceAdviceFile"/> instance that represents the parsed file.</returns>
        Task<RemittanceAdviceFile> ParseRemittanceAdviceFile(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the specified <paramref name="lines"/> to the specified <paramref name="path"/> formatted as an inbound 835 payment posted file.
        /// </summary>
        /// <param name="path">The path of the file write.</param>
        /// <param name="lines">The lines to write.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task WritePaymentPostedFile(string path, IEnumerable<PaymentPostedFile.Line> lines, CancellationToken cancellationToken = default);
    }
}
