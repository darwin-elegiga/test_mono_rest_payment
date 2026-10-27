using System.IO;

namespace VPay.Optum.Nc835FileProcessor.Application.Services
{
    public interface IX12Parser
    {
        IX12Reader GetReader(Stream stream);
    }
}
