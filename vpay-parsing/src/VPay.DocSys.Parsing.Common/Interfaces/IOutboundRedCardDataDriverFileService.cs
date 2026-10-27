using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IOutboundRedCardDataDriverFileService : IDriverFileService
    {
        Task<RedCardProcessResult> ParseFile(string outboundRedCardDataDriverFileName);

        Task QueueFileForParsing(string outboundRedCardDataDriverFileName);
    }
}
