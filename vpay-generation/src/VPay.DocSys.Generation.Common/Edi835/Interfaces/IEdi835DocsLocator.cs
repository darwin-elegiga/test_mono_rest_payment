using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Generation.Common.Edi835;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    /// <summary>
    /// All implementors of this interface should take the Edi835CreationMessage
    /// and return a string represention of the contents of the particular filetype being
    /// implemented.
    /// </summary>
    public interface IEdi835DocsLocator
    {
        Task<DocsFile> LocateContent(DS.Edi835 message, CancellationToken cancellationToken);
    }
}
