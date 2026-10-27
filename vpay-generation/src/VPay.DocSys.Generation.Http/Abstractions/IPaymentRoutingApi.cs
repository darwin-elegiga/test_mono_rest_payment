using Refit;
using VPay.DocSys.Generation.Common.Models;

namespace VPay.DocSys.Generation.Http.Abstractions
{
    public interface IPaymentRoutingApi
    {
        [Get("/api/about")]
        Task GetAbout();

        [Get("/api/Support/incompleteBatchesCount?minutesLag={minutesLag}")]
        Task<int> GetIncompleteBatchesCount(int minutesLag, CancellationToken cancellationToken = default);

        [Get("/api/Outbound835/transaction/{transactionId}/can-download")]
        Task<Outbound835ExistsDto> CanDownloadOutbound835Document(long transactionId, CancellationToken cancellationToken);

        [Get("/api/Outbound835/transaction/{transactionId}/download")]
        Task DownloadOutbound835Document(long transactionId, CancellationToken cancellationToken);
    }
}
