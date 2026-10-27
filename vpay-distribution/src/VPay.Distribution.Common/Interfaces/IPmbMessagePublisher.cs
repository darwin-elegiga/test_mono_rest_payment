using System.Threading;
using System.Threading.Tasks;

namespace VPay.Distribution.Common.Interfaces
{
    public interface IPmbMessagePublisher
    {
        Task SendAsync<TPmbMessage>(TPmbMessage message, CancellationToken cancellationToken)
             where TPmbMessage : IPmbMessage;        
    }
}
