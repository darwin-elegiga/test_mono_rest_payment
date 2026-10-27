using System.Threading.Tasks;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Warranty Service for PDF Generation
    /// </summary>
    public interface IFaxMessageService
    {


        /// <summary>
        /// Creates a pdf as a file in the same location as the xml file
        /// </summary>
        /// <param name="xmlFilePath">xml file path</param>
        /// <returns>file path of pdf</returns>
         Task<string> CreateWarrantyPdfFromXmlFile(string xmlFilePath);

        /// <summary>
        /// Creates a pdf as a file in the same location as the xml file
        /// </summary>
        //// <param name="xmlFilePath">xml file path</param>
        /// <returns>file path of pdf</returns>
        Task<string> CreateEftFaxPdfFromXmlFile(string xmlFilePath);


        /// <summary>
        /// Creates an archive file of the Eft Fax xml and Eft Fax pdf created
        /// </summary>
        /// <param name="xmlString">xml as string</param>
        /// <returns>archive file</returns>
        Task<IArchiveFile> CreateArchiveForEftFaxXmlString(string xmlString);

        /// <summary>
        /// Creates an archive file of the Eft Fax xml and Eft Fax pdf created
        /// </summary>
        /// <param name="xmlString">xml as string</param>
        /// <returns>archive file</returns>
        Task<IArchiveFile> CreateArchiveForWarrantyXmlString(string xmlString);

    }
}
