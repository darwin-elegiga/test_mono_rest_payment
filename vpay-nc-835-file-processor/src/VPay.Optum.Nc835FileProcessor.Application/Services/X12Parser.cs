using System.IO;

namespace VPay.Optum.Nc835FileProcessor.Application.Services
{
    public class X12Parser : IX12Parser
    {
        public IX12Reader GetReader(Stream stream)
        {
            return new X12Reader(stream);
        }
    }
}
