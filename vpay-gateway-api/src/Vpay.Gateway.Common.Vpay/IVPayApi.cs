using Refit;
using Vpay.Gateway.Common.Vpay.Models;

namespace Vpay.Gateway.Common.Vpay;

/// <summary>
/// Interface defining the shape of the VPay api.
/// </summary>
public interface IVPayApi
{
    /// <summary>
    /// Performs a search for transactions matching the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object containing information about the search to be performed.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <returns>The result of the search.</returns>
    [Post("/txn/search")]
    Task<SearchResult> PostTransactionSearch(TransactionSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a search for transactions matching the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The object containing information about the search to be performed.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <returns>The result of the search.</returns>
    [Get("/txn/{transactionId}")]
    Task<TransactionSearchResponse> GetTransactionDetailsByTransactionID([Query] long transactionId, bool detail = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the transaction chain details for a given transaction Id.
    /// </summary>
    /// <param name="transactionId">The transaction Id to retrieve the chain details for all transactions associated</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <returns>The transaction chain details.</returns>
    [Get("/txn/{transactionId}/chain")]
    Task<List<ReissueTransactionIDs>> GetTransactionChain(long transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the transaction correspondance details for a given transaction Id.
    /// </summary>
    /// <param name="transactionId">The transaction Id to retrieve the correspondance details for all transactions associated</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <returns>The transaction correspondance details.</returns>
    [Get("/txn/{transactionId}/corr")]
    Task<List<Correspondence>> GetTranCorrespondanceDetails(long transactionId, bool detail = true, CancellationToken cancellationToken = default);
}
