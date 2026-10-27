using Refit;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Common.Kubera;

public interface IKuberaApi
{
    [Get("/api/transactions")]
    Task<Transaction[]> GetTransactionsByIds([Query(CollectionFormat.Multi)] long[] ids, CancellationToken cancellationToken = default);

    [Get("/api/transactions")]
    Task<Transaction[]> GetTransactionsByRange(long? startId = null, long? endId = null, CancellationToken cancellationToken = default);

    [Get("/api/transactions/clients/{clientCode}")]
    Task<Transaction[]> GetTransactionsByPaymentId(string clientCode, string paymentId, CancellationToken cancellationToken = default);
}
