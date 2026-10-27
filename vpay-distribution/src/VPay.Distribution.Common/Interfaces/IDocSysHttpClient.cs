using System.Threading.Tasks;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    public interface IDocSysHttpClient
    {
        Task<DocSysConfiguration> GetDocSysConfig(string tpa);
        IDocSysConfigCache DocSysConfigCache { get; }
    }
}
