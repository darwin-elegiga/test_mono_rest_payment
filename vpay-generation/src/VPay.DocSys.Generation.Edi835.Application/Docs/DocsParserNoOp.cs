using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserNoOp : IEdi835Creator, IEdi835DocsLocator
    {
        public DocsParserNoOp()
        {
        }

        public Task<DocsFile> LocateContent(DS.Edi835 message, CancellationToken cancellationToken = default)
        {
            var docsFile = new DocsFile { Content = "", Filename = message.GetTempFilename("docs") };
            return Task.FromResult(docsFile);
        }

        public Task<string> CreateContent(Edi835CreationMessage message, CancellationToken cancellationToken = default)
        {
            return Task.FromResult( string.Empty );
        }
    }
}
