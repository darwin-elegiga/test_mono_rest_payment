using System.Threading;
using System.Threading.Tasks;
using VPay.Data.Db2.Abstractions.Trn835Data;

namespace VPay.DocSys.Generation.Edi835.Application
{
    public class TrnService : ITrnService
    {
        private readonly ITrn835DataRepository _repository;

        public TrnService(ITrn835DataRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> GenerateTrnDocument(long nonFundedPaymentDetailId, CancellationToken cancellationToken)
        {
            var detail = await _repository.GetTrn835DataByNonFundedPaymentDetailId(nonFundedPaymentDetailId, cancellationToken).ConfigureAwait(false);
            return (detail?.Segment ?? string.Empty) + "\n"; 
        }
    }
}
