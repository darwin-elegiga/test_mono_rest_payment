using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is used to encapsulate <see cref="ImageMagick"/> operations
    /// to convert pdf images to tiff images as required for faxes.
    /// </summary>
    public interface ITiffOperations
    {

        /// <summary>
        /// Creates a multi-page single tiff file from the given multi-page pdf.
        /// </summary>
        /// <param name="pdfFilePath">pdf file path</param>
        /// <returns>tiff file path</returns>
        Task<string> CreateTiffFromPdf(string pdfFilePath);
    }
}
