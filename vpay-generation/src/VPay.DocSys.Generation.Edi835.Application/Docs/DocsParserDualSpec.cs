using System.IO.Abstractions;
using VPay.DocSys.Generation.Common.Edi835;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserDualSpec : DocsParserDirectSpec
    {
        public DocsParserDualSpec(IFileSystem fileSystem, Edi835ServiceSettings edi835ServiceSettings)
            : base(fileSystem, edi835ServiceSettings)
        {
        }
    }
}
