using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Edi835.Application
{
    public interface ITrnService
    {
        Task<string> GenerateTrnDocument(long nonfundedPaymentDetailId, CancellationToken cancellationToken);
    }
}
