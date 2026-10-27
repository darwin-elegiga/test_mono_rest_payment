using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public interface IDropToPrintService
    {
        Task<int> CallDropToPrintPython(string statusFilePath);
    }
}
