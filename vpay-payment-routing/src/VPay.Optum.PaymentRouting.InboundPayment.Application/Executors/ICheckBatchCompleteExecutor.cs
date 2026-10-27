using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;

namespace VPay.Optum.PaymentRouting.InboundPayment.Application.Executors
{
    public interface ICheckBatchCompleteExecutor
    {
        public Task<Unit> Execute(BatchSummary batchSummary, CancellationToken cancellation = default);
    }
}
