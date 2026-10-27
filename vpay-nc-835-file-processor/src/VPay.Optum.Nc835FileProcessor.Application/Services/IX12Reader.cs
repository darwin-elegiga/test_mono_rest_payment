using System.Collections.Generic;

namespace VPay.Optum.Nc835FileProcessor.Application.Services
{
    public interface IX12Reader
    {
        IAsyncEnumerable<object> GetNext();
    }
}
