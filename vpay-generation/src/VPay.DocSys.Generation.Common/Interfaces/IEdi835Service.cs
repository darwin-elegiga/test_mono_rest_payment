using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Common
{
    public interface IEdi835Service
    {
        void QueueMessage(DS.Edi835 edi835);
        Task<Dictionary<long, string>> PublishTransactions(IEnumerable<long> transactionList, string user, string programName, CancellationToken cancellationToken);
    }
}
