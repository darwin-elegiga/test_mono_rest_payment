using Refit;

namespace VPay.Gateway.Common.Loaf;

/// <summary>
/// Interface defining the calls in the Loaf API that are used
/// </summary>
public interface ILoafApi
{
    [Get("/api/CardInformation/TransactionId/{transactionId}")]
    Task<IApiResponse<CardDetailDto?>> TransactionInfo(long transactionId, CancellationToken cancellationToken);
}
