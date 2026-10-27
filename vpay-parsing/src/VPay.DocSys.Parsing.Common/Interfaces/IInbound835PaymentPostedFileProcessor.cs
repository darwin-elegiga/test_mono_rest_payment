using System;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// An <see cref="IInbound835FileProcessor"/> that processes <see cref="Inbound835FileType.PaymentPosted"/> files.
    /// </summary>
    public interface IInbound835PaymentPostedFileProcessor : IInbound835FileProcessor
    {
    }
}
