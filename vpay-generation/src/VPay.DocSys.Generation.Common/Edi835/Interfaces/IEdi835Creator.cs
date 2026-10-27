using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Edi835
{
    /// <summary>
    /// All implementors of this interface should take the Edi835CreationMessage
    /// and return a string represention of the contents of the particular filetype being
    /// implemented.
    /// </summary>
    public interface IEdi835Creator
    {
        Task<string> CreateContent(Edi835CreationMessage message, CancellationToken cancellationToken);
    }
}
