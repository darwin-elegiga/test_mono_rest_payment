using Refit;
using VPay.Gateway.Common.Documents.Models;

namespace VPay.Gateway.Common.Documents;

/// <summary>
/// Interface defining the shape of the Documents API.
/// </summary>
public interface IDocumentsApi
{
    /// <summary>
    /// Retrieves all records about archive files with the supplied <paramref name="transactionId"/> (in theory, should only be one, but may be more, depending on reissues, and/or other factors)
    /// </summary>
    /// <param name="transactionId">The transaction id to retrieve the files for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <returns>The file information.</returns>
    [Get("/api/Storage/Transaction/{transactionId}/Archive")]
    Task<ArchiveFile[]> ListDocuments(long transactionId, CancellationToken cancellationToken);

    /// <summary>
    /// Download as single file from an archive, based on the <paramref name="archiveDetailId"/>.
    /// </summary>
    /// <param name="archiveDetailId">The id to retrieve the file for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <returns>The file.</returns>
    [Get("/api/Storage/Archive/Detail/{archiveDetailId}/Download")]
    Task<HttpContent> DownloadFile(long archiveDetailId, CancellationToken cancellationToken);

    /// <summary>
    /// Download the VPayge + EOB as single file from an archive, based on the TransactionId and the PaymentType.
    /// </summary>
    /// <param name="transactionId">The transaction id to retrieve the file for</param>
    /// <param name="paymentType">The Payment Type of the file</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Get("/api/Storage/Archive/Detail/Download/{transactionId}/{paymentType}/DistributionPacket")]
    Task<ApiResponse<HttpContent>> DownloadDistributionPacket(long transactionId, string paymentType, CancellationToken cancellationToken);

    /// <summary>
    /// Download the masked VPayge + EOB as single file from an archive, based on the TransactionId and the PaymentType.
    /// </summary>
    /// <param name="transactionId">The transaction id to retrieve the file for</param>
    /// <param name="paymentType">The Payment Type of the file</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Get("/api/Storage/Archive/Detail/Download/{transactionId}/{paymentType}/MaskedDistributionPacket")]
    Task<ApiResponse<HttpContent>> DownloadMaskedDistributionPacket(long transactionId, string paymentType, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the 835 from document system, based on the <paramref name="transactionId"/>
    /// </summary>
    /// <param name="transactionId">The transaction id to retrieve the file for</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Get("/api/Storage/Archive/Detail/Download/{transactionId}/Edi835")]
    Task<HttpContent> Download835(long transactionId, CancellationToken cancellationToken);


    /// <summary>
    /// Downloads the 835 from document system, based on the <paramref name="transactionId"/>
    /// </summary>
    /// <param name="transactionId">The transaction id to retrieve the file for</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Get("/api/DocumentTransaction/GetPaymentByTransactionID/{transactionId}")]
    Task<TransactionPayment?> DocumentPaymentByTransactionID(long transactionId, CancellationToken cancellationToken);
}
