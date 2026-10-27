using Refit;
using VPay.Distribution.Common;

namespace VPay.Distribution.Http.Abstractions
{
    public interface IPaymentRoutingApi
    {
        [Get("/api/about")]
        Task GetAbout();

        [Get("/api/Support/incompleteBatchesCount?{minutesLag}")]
        Task<int> GetIncompleteBatchesCount(int minutesLag, CancellationToken cancellationToken);

        [Get("/api/Outbound835/transaction/{transactionId}/can-download")]
        Task<Outbound835ExistsDto> CanDownloadOutbound835Document(long transactionId, CancellationToken cancellationToken);

        [Get("/api/Outbound835/transaction/{transactionId}/download")]
        Task DownloadOutbound835Document(long transactionId, CancellationToken cancellationToken);
    }
}
