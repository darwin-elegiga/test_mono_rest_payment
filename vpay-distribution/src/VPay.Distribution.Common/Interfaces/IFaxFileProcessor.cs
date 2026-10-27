using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public interface IFaxFileProcessor
    {
        Task<DistributionProcessResult> Process(DocDistMsg message, bool createControlFile = true);
    }
}
