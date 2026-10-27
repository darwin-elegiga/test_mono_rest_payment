using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface IStatusFileService
    {
        /// <summary>
        /// Process data required for document generation for each transaction in a status file and send it to a queue
        /// </summary>
        /// <param name="fileName"></param>
        Task ParseAndQueueFile(string fileName);
        Task ParseAndQueueTransactions(string stsFilePath, IList<long> transactionIds, bool isRegenRequest);

        Task QueueFileForParsing(string statusFilePath, IList<long> transactionIds, bool isRegenRequest, bool isEmergencyFile = false);

        /// <summary>
        /// Create a json string payload of <see cref="DocDistMsg"/> for <param name="statusFilePath"></param>
        /// </summary>
        /// <param name="statusFilePath"></param>
        /// <param name="distType"></param>
        /// <returns></returns>
        Task<string> GenerateDistributionMessage(string statusFilePath, string distType);
        Task RegenerateAuxFile(string statusFilePath);
        /// <summary>
        /// Create a json string payload of <see cref="DocDistMsg"/> for <param name="statusFilePath"> and publishes the <see cref="DocDistMsg"/> to the DistributionQueue</param>
        /// </summary>
        /// <param name="statusFilePath"></param>
        /// <param name="distType"></param>
        /// <returns>The Serialized <see cref="DocDistMsg"/></returns>
        Task<string> GenerateAndPublishDistributionMessage(string statusFilePath, string distType);

        /// <summary>
        /// Insert STP payment records into database table.
        /// STP payments are identified by having the Network Type of 'STP'.
        /// STP records are extracted from the Status File and grouped by Network Name and Enrollment Type.
        /// The EDI 835 type is determined by checking if the client listed in the Status File is for a PRS or SDS account.
        /// Each distinct group results in a separate bulk insert operation into the database.
        /// </summary>
        /// <param name="statusFile">The status file containing zero or more STP payment records.</param>
        /// <param name="originCode">The origin code from the status file.</param>
        /// <param name="clientCode">The client code from the status file.</param>
        /// <param name="billingEntityCode">The billing entity code from the status file.</param>
        Task<IEnumerable<long>> CreateStpPaymentRecords(StatusFile statusFile, string originCode, string clientCode, string billingEntityCode);

        /// <summary>
        /// Determine if EDI 835 type is PRS or SDS based on the payment client information.
        /// </summary>
        /// <param name="originCode">The origin code from the status file.</param>
        /// <param name="clientCode">The client code from the status file.</param>
        /// <param name="billingEntityCode">The billing entity code from the status file.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The EDI 835 type: PRS or SDS.</returns>
        Task<string> GetEdi835Type(string originCode, string clientCode, string billingEntityCode, CancellationToken cancellationToken = default);
    }
}
