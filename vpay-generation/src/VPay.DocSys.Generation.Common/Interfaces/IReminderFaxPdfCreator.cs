using System.IO;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Reminder Fax PDF Creator
    /// </summary>
    public interface IReminderFaxPdfCreator
    {
        /// <summary>
        /// Creates a pdf from the <see cref="CoverSheetParameters"/>
        /// </summary>
        /// <param name="coverSheetParameters">values to create the reminder fax cover sheet</param>
        /// <returns><see cref="MemoryStream"/> of pdf bytes</returns>
        Task<MemoryStream> CreatePdfStream(CoverSheetParameters coverSheetParameters);
    }
}
