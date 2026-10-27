using System.Threading.Tasks;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common.Interfaces
{
    /// <summary>
    /// Warranty Email Service for PDF Generation
    /// </summary>
    public interface IEmailMessageService
    {
        /// <summary>
        /// Creates an archive file of the EVpayge pdf created
        /// </summary>
        /// <param name="xmlString">xml as string</param>
        /// <returns>archive file</returns>
        Task<IArchiveFile> CreateArchiveForWarrantyXmlString(string xmlString);
    }
}
