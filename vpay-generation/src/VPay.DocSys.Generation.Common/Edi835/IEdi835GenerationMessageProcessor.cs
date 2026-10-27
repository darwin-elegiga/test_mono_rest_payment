using System;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public interface IEdi835GenerationMessageProcessor
    {
        Task ProcessMessage(string messsage, CancellationToken cancellationToken);
    }
}
