using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    /// <summary>
    /// Service class for parsing inv.drv files for RedCard
    /// </summary>
    public interface IInventoryDriverFileService : IDriverFileService
    {
        /// <summary>
        /// Gets or sets a value indicating whether transactions in the driver file that do not exist in DocSys should be ignored.
        /// </summary>
        /// <value>
        ///   <c>true</c> if missing transactions should be ignored; otherwise, <c>false</c>.
        /// </value>
        bool IgnoreMissingTransactions { get; set; }

        /// <summary>
        /// Will parsing the inv.drv file and its corresponding tar file.
        /// Takes into account if the records are from the legacy generation process or not and acts accordingly
        /// </summary>
        /// <param name="inventoryDriverFileName">inv.drv file name</param>
        /// <returns>result of processing the file</returns>
        Task<RedCardProcessResult> ParseFile(string inventoryDriverFileName);

        /// <summary>
        /// Will queue to file for parsing
        /// </summary>
        /// <param name="inventoryDriverFileName">inv.drv filename</param>
        /// <returns></returns>
        Task QueueFileForParsing(string inventoryDriverFileName);
    }
}
