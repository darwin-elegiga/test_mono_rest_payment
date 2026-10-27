using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public interface IMailFileProcessor
    {
        Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true);
    }
}
