using System.IO;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IOutboundRedCardDataDriverStatusWriter
    {
        void Write(TextWriter textWriter, OutboundRedCardDataDriverFile outboundRedCardDataDriverFile);
    }
}
