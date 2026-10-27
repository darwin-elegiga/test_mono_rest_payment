using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using DS=VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835.Application.PayFile
{
    public interface IPayfileService
    {
        Task<string> GeneratePayFileDocument(DS.Edi835 message, CancellationToken cancellationToken);
    }
}
