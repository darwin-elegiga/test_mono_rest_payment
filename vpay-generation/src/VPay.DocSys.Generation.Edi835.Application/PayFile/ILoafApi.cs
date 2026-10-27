using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Generation.Edi835.Application.Models;

namespace VPay.DocSys.Generation.Edi835.Application.PayFile
{
    public interface ILoafApi
    {
        Task<LoafTransactionBankInfoDto> GetOptumBankInfo(string transactionId, CancellationToken cancellationToken);
    }
}
