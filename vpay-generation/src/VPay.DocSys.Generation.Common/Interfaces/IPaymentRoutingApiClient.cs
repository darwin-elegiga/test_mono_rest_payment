using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Generation.Common.Models;

namespace VPay.DocSys.Generation.Common.Interfaces
{
    public interface IPaymentRoutingApiClient
    {
        Task<Outbound835DocumentResponse> GetOutbound835Document(long transactionId, CancellationToken cancellationToken);
    }
}
