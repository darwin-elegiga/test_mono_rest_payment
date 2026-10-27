using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace VPay.DocSys.Parsing;

public interface IPaymentEngineConfirmationFileSender
{
    Task SendFileToTransferService(IFileInfo fileInfo, string trackingId, CancellationToken cancellationToken);
}
