using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/text")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class Edi835SupportController : ControllerBase
    {
        private readonly ILogger<Edi835SupportController> _logger;
        private readonly IEdi835CreationService _edi835CreationService;
        private readonly IEdi835Repository _edi835Repository;
        private readonly IPrfTransactionRepository _prfTransactionRepository;
        private readonly IDistributionRepository _distributionRepository;
        private readonly IEdi835Service _edi835GenerationMessagePublisher;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;
        private readonly string _programName;
        private readonly string _user;
        public Edi835SupportController(
            ILogger<Edi835SupportController> logger,
            IEdi835CreationService edi835CreationService,
            IEdi835Repository edi835Repository,
            IPrfTransactionRepository prfTransactionRepository,
            IDistributionRepository distributionRepository,
            IEdi835Service edi835GenerationMessagePublisher,
            Edi835ConfigurationCache edi835ConfigurationCache
            )
        {
            _logger = logger;
            _edi835CreationService = edi835CreationService;
            _edi835Repository = edi835Repository;
            _prfTransactionRepository = prfTransactionRepository;
            _distributionRepository = distributionRepository;
            _edi835GenerationMessagePublisher = edi835GenerationMessagePublisher;
            _programName = GetType().Name;
            _user = HttpContext?.User?.Identity?.Name ?? "System";
            _edi835ConfigurationCache = edi835ConfigurationCache;
        }

        #region Create components of the 835 documents

        #region Old methods that take the big json stuff

        /// <summary>
        /// Will generate a single .docs file for the given DocGenParam.
        /// </summary>
        [HttpPost("CreateDocsFile")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateDocsFile(DS.Edi835 message)
        {
            var result = await _edi835CreationService.GenerateDocsFile(message);
            if (string.IsNullOrEmpty(result.Content))
            {
                return NotFound();
            }
            byte[] allBytes = Encoding.UTF8.GetBytes(result.Content);
            return File(allBytes, "application/text", result.Filename);
        }

        /// <summary>
        /// Will generate a single .payfile file for the given DocGenParam.
        /// </summary>
        [HttpPost("CreatePayFile")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreatePayFile(DS.Edi835 message)
        {
            var result = await _edi835CreationService.GeneratePayfileFile(message);
            if (string.IsNullOrEmpty(result.Content))
            {
                return NotFound();
            }
            byte[] allBytes = Encoding.UTF8.GetBytes(result.Content);
            return File(allBytes, "application/text", result.Filename);
        }

        /// <summary>
        /// Will generate a single .trn file for the given message.
        /// </summary>
        /// <param name="message">Edi835CreationMessage created from the parsing endpoint.</param>
        [HttpPost("CreateTrnFile")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTrnFile(DS.Edi835 message)
        {
            var result = await _edi835CreationService.GenerateTrnFile(message);
            if (string.IsNullOrEmpty(result.Content))
            {
                return NotFound();
            }
            byte[] allBytes = Encoding.UTF8.GetBytes(result.Content);
            return File(allBytes, "application/text", result.Filename);
        }

        /// <summary>
        /// Attempts to find the PDF for the given file.  If not on Production, will return "Fake PDF" with the appropriate 
        /// </summary>
        /// <remarks></remarks>
        /// <param name="message">Edi835CreationMessage created from the parsing endpoint.</param>
        [HttpPost("FetchPdfFor")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/octet-stream", "text/plain")]
        public async Task<IActionResult> FetchPdfFor(DS.Edi835 message)
        {


            using (var archiveDetailStream = await _edi835CreationService.FetchPdfFor(message))
            {
                if (archiveDetailStream == null)
                {
                    return NotFound();
                }

                // write archive detail to memory buffer so we can close/dispose the ArchiveDetailStream
                var memoryStream = new MemoryStream();
                using (var fileStream = archiveDetailStream.Open())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    fileStream.Close();
                }
                memoryStream.Position = 0;

                // return FileStreamResult from memory stream
                return File(memoryStream, archiveDetailStream.ContentType, archiveDetailStream.FileName);
            }
        }

        /// <summary>
        /// Will generate the .payfile, .trn and .docs files, and fetch the .pdf file
        /// for the given <see cref="Edi835CreationMessage"/>
        /// </summary>
        /// <param name="message"><see cref="Edi835CreationMessage"/> indicating a single transaction.</param>
        /// <returns></returns>
        [HttpPost("GenerateTar")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/x-tar")]
        public async Task<IActionResult> GenerateTar(DS.Edi835 message)
        {
            var tarPackage = await _edi835CreationService.GenerateTarPackage(message);
            return File(tarPackage.Stream, tarPackage.ContentType, tarPackage.Filename);
        }

        #endregion
        #region New methods that just take transactionId 


        /// <summary>
        /// Will generate a single .docs file for the given DocGenParam.
        /// </summary>
        [HttpPost("CreateDocsFileByTransactionId")]
        [Produces("application/text", "application/json")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateDocsFileByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {
            var message = await GetEdi835Internal(transactionId, cancellationToken);
            if (message == null)
            {
                return NotFound();
            }

            var result = await _edi835CreationService.GenerateDocsFile(message);
            if (string.IsNullOrEmpty(result.Content))
            {
                return NotFound();
            }
            byte[] allBytes = Encoding.UTF8.GetBytes(result.Content);
            return File(allBytes, "application/text", result.Filename);
        }

        /// <summary>
        /// Will generate a single .payfile file for the given DocGenParam.
        /// </summary>
        [HttpPost("CreatePayFileByTransactionId")]
        [Produces("application/text", "application/json")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreatePayFileByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {
            var message = await GetEdi835Internal(transactionId, cancellationToken);
            if (message == null)
            {
                return NotFound();
            }
            var result = await _edi835CreationService.GeneratePayfileFile(message).ConfigureAwait(false);
            if (string.IsNullOrEmpty(result.Content))
            {
                return NotFound();
            }
            byte[] allBytes = Encoding.UTF8.GetBytes(result.Content);
            return File(allBytes, "application/text", result.Filename);
        }

        /// <summary>
        /// Will generate a single .trn file for the given message.
        /// </summary>
        [HttpPost("CreateTrnFileByTransactionId")]
        [Produces("application/text", "application/json")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTrnFileByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {
            var message = await GetEdi835Internal(transactionId, cancellationToken);
            if (message == null)
            {
                return NotFound();
            }
            var result = await _edi835CreationService.GenerateTrnFile(message).ConfigureAwait(false);
            if (string.IsNullOrEmpty(result.Content))
            {
                return NotFound();
            }
            byte[] allBytes = Encoding.UTF8.GetBytes(result.Content);
            return File(allBytes, "application/text", result.Filename);
        }

        /// <summary>
        /// Attempts to find the PDF for the given file.  If not on Production, will return "Fake PDF" with the appropriate 
        /// </summary>
        [HttpPost("FetchPdfByTransactionId")]
        [Produces("application/octet-stream", "application/json", "text/plain")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FetchPdfForByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {

            var message = await GetEdi835Internal(transactionId, cancellationToken);
            if (message == null)
            {
                return NotFound();
            }
            using (var archiveDetailStream = await _edi835CreationService.FetchPdfFor(message).ConfigureAwait(false))
            {
                if (archiveDetailStream == null)
                {
                    return NotFound();
                }

                // write archive detail to memory buffer so we can close/dispose the ArchiveDetailStream
                var memoryStream = new MemoryStream();
                using (var fileStream = archiveDetailStream.Open())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    fileStream.Close();
                }
                memoryStream.Position = 0;

                // return FileStreamResult from memory stream
                return File(memoryStream, archiveDetailStream.ContentType, archiveDetailStream.FileName);
            }
        }

        /// <summary>
        /// Will generate the .payfile, .trn and .docs files, and fetch the .pdf file
        /// for the given <see cref="Edi835CreationMessage"/>
        /// </summary>
        [HttpPost("GenerateTarByTransactionId")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/x-tar", "application/json", "text/plain")]
        public async Task<IActionResult> GenerateTarByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {
            var message = await GetEdi835Internal(transactionId, cancellationToken);
            if (message == null)
            {
                return NotFound();
            }
            var tarPackage = await _edi835CreationService.GenerateTarPackage(message).ConfigureAwait(false);
            return File(tarPackage.Stream, tarPackage.ContentType, tarPackage.Filename);
        }



        #endregion

        #endregion


        #region Get information about Edi835s in the system
        private async Task<DS.Edi835> GetEdi835Internal(long transactionId, CancellationToken cancellationToken = default)
        {
            return await _edi835Repository
                .GetMostRecent835(transactionId, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the Edi835 Message from the database for a given TransactionId
        /// </summary>
        [HttpGet("Edi835/transactionId/{transactionId}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DS.Edi835), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEdi835(long transactionId, CancellationToken cancellationToken = default)
        {
            var edi835 = await GetEdi835Internal(transactionId, cancellationToken);
            if (edi835 == null)
            {
                return NotFound();
            }
            return Ok(edi835);
        }


        /// <summary>
        /// Gets the current status of a transaction as it relates to the Edi835 process.
        /// This will reference the most recently created metadata record.
        /// </summary>
        /// <returns></returns>
        [HttpGet("Edi835Status/current/transactionId/{transactionId}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Edi835Status), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetEdi835StatusByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {
            var edi835 = await _edi835Repository.GetMostRecent835(transactionId, cancellationToken).ConfigureAwait(false);
            if (edi835 == null)
            {
                return NotFound();
            }
            var status = await _edi835Repository.GetCurrent835Status(edi835.Edi835Id, cancellationToken).ConfigureAwait(false);
            if (status == null)
            {
                return NotFound();
            }
            return Ok(status);
        }

        /// <summary>
        /// Gets the current status of a specific Edi835 instance. 
        /// </summary>
        /// <returns></returns>
        [HttpGet("Edi835Status/current/{edi835Id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Edi835Status), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetEdi835StatusByEdi835Id(long edi835Id, CancellationToken cancellationToken = default)
        {

            var status = await _edi835Repository.GetCurrent835Status(edi835Id, cancellationToken).ConfigureAwait(false);
            if (status == null)
            {
                return NotFound();
            }
            return Ok(status);
        }

     


        /// <summary>
        /// Gets the complete status history for a single Edi835 request.
        /// </summary>
        /// <param name="edi835Id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("Edi835Status/history/{edi835Id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IList<Edi835Status>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public async Task<IActionResult> Edi835StatusHistory(long edi835Id, CancellationToken cancellationToken = default)
        {
            var history = await _edi835Repository.Get835StatusHistory(edi835Id, cancellationToken);
            return Ok(history);
        }

        /// <summary>
        /// Gets the complete status history for the most recent Edi835 for a TransactionId.
        /// </summary>
        [HttpGet("Edi835Status/history/transactionId/{transactionId}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IList<Edi835Status>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Edi835StatusHistoryByTransactionId(long transactionId, CancellationToken cancellationToken = default)
        {
            var edi835 = await GetEdi835Internal(transactionId, cancellationToken);
            if (edi835 == null )
            {
                return NotFound();
            }
            var history = await _edi835Repository.Get835StatusHistory(edi835.Edi835Id, cancellationToken);
            if ( history == null )
            {
                return NotFound();
            }
            return Ok(history);
        }

        /// <summary>
        /// Gets the complete status history for the all Edi835s for a TransactionId.
        /// </summary>
        [HttpGet("Edi835Status/history/transactionId/{transactionId}/all")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Edi835StatusHistory>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Edi835StatusHistoryByTransactionIdAll(long transactionId, CancellationToken cancellationToken = default)
        {
            var allEdi835s = await _edi835Repository.GetAll835s(transactionId, cancellationToken);
            if (allEdi835s == null)
            {
                return NotFound();
            }
            var historyList = new List<Edi835StatusHistory>();
 
            foreach (var e in allEdi835s)
            {
               
                var history = await _edi835Repository.Get835StatusHistory(e.Edi835Id, cancellationToken);
                historyList.Add(new Edi835StatusHistory(e, history.ToList()));
            }
 
            return Ok(historyList);
        }




        #endregion

        #region Methods to manipulate Edi835s and transactions



        /// <summary>
        /// Updates the statuses for the current Edi835s for all of the given TransactionIds
        /// </summary>
        /// <remarks>
        /// Possible values for Edi835StatusCode are:
        /// <list type="bullet">
        /// <item>Initiated = 0,                      </item>
        /// <item>QueuedForGeneration = 100,          </item>
        /// <item>Generating = 200,                   </item>
        /// <item>GenerationComplete = 300,           </item>
        /// <item>QueuedForDistribution = 400,        </item>
        /// <item>SentToVendor = 500,                 </item>
        /// <item>VendorAcknowledged = 600,           </item>
        /// <item>UnknownRemarkCode = 700,            </item>
        /// <item>Received835FromVendor = 800,        </item>
        /// <item>Sent835ToAvaility = 900,            </item>
        /// <item>VendorPostedPayment = 1000,         </item>
        /// <item>VendorReportedError = 1100,         </item>
        /// <item>ProviderCancelledEnrollment = 1200, </item>
        /// <item>UnknownVendorResponse = 1300,       </item>
        /// <item>CancelledByUser = 1400,             </item>
        /// <item>WillNotReceive835 = 1500            </item>
        /// </list>
        /// </remarks>

        [HttpPost("Edi835BulkUpdateStatus")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Edi835BulkUpdateStatus(Edi835BulkUpdateRequest request, CancellationToken cancellationToken = default)
        {
             return await BulkUpdateStatus(request, "Edi835BulkUpdateStatus", cancellationToken).ConfigureAwait(false);
        }



        /// <summary>
        /// Overload of Edi835BulkUpdateStatus, with the 1400/WillNotReceive835 value set for all transactions.
        /// </summary>
        [HttpPost("Edi835BulkUpdateStatus/WillNotReceive835")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Edi835BulkUpdateStatus_WillNotReceive835(Edi835BulkUpdateRequest_WillNotReceive835 request, CancellationToken cancellationToken = default)
        {
            Edi835BulkUpdateRequest newRequest = new Edi835BulkUpdateRequest
            {
                TransactionIds = request.TransactionIds,
                UserName = request.UserName,
                Comment = request.Comment,
                Edi835StatusCode = Edi835StatusCode.WillNotReceive835
            };
            return await BulkUpdateStatus(newRequest, "Edi835BulkUpdateStatus_WillNotReceive835", cancellationToken).ConfigureAwait(false);
 
        }


        private async Task<IActionResult> BulkUpdateStatus(Edi835BulkUpdateRequest request, string methodName, CancellationToken cancellationToken)
        {

            string comment = (!string.IsNullOrWhiteSpace(request.Comment) && !(request.Comment == "string")) ? request.Comment : "";
            string user = (!string.IsNullOrWhiteSpace(request.UserName) && !(request.UserName == "string")) ? request.UserName : _user;

            var edi835s = await _edi835Repository.GetMostRecent835s(request.TransactionIds, cancellationToken).ConfigureAwait(false);
            if (edi835s?.Count <= 0)
            {
                return NotFound();
            }

            await _edi835Repository.Add835Status(edi835s.Select(e => e.Edi835Id), request.Edi835StatusCode, comment, methodName, _programName, user, cancellationToken).ConfigureAwait(false);
            return Ok();
        }

        /// <summary>
        /// Cancel (set to terminal) a all Edi835 records for the given transactionIds.  This will effectively prevent a given request from
        /// flowing through the system without creating a new request.
        /// </summary>
        /// <returns>200OK</returns>
        [HttpPost("CancelEdi835Status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelAllEdi835sForTransactionIds(TransactionIdListRequest request, CancellationToken cancellationToken = default)
        {
            await _edi835Repository.TerminateEdi835sByTransactionIds(request.TransactionIds, _programName, _user, cancellationToken).ConfigureAwait(false);
            return Ok();
        }

        /// <summary>
        /// Cancel (set to terminal) all Edi835 records for the given transactionIds, then immediately creates new Edi835 records set to Initiated.
        /// NOTE: this WILL NOT queue the request for generation.
        /// Gives a "stopping" point to allow ProdSupport or QA to
        /// handle any manual situation they would need to manage.
        /// </summary>
        /// <returns>200OK</returns>
        [HttpPost("ResetEdi835s")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(typeof(IList<DS.Edi835>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetEdi835s(TransactionIdListRequest request, CancellationToken cancellationToken = default)
        {
            var allDocs = await _distributionRepository.GetPayments(request.TransactionIds, cancellationToken);
            if ( (allDocs?.Count??0) == 0 )
            {
                return NotFound("Transactions not found in DocumentSystem.");
            }
            var firstClientCode = allDocs.FirstOrDefault().TPA;
            if (! allDocs.All(_=>_.TPA== firstClientCode) )
            {
                return BadRequest("All transactions must belong to the same ClientCode.");
            }

            var edi835List = await _edi835Repository.Create835s(request.TransactionIds, _programName, _user, cancellationToken);
            return Ok(edi835List);
        }


        const int maxTransactions = 100;
        /// <summary>
        /// Given a list of transactionIds, this will check if each is available to be queued for Edi835Generation and will queue them if they are.
        /// Due to timeout considerations (this is an intensive operation), only 100 transactions can be submitted at a time.  Please split your lists
        /// and submit accordingly if needed.
        /// </summary>
        /// <returns></returns>
        [HttpPost("PublishTransactions")]
        [Produces("application/json", "application/text")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        public async Task<ActionResult> PostPublishTransactions(TransactionIdListRequest request, CancellationToken cancellationToken = default)
        {
            var scrubbedTransactionList = request.TransactionIds.Where(t => t > 0).Distinct();

            if (scrubbedTransactionList.Count() > maxTransactions)
            {
                return StatusCode(
                    StatusCodes.Status413RequestEntityTooLarge,
                    $"Due to timeout considerations, no more than {maxTransactions} transactions can be submitted at one time. Please split your query into lists of {maxTransactions} or less."
                );
            }

            var resultSet = await _edi835GenerationMessagePublisher.PublishTransactions(scrubbedTransactionList, _user, _programName, cancellationToken);

            return Accepted(resultSet);
        }

        /// <summary>
        /// This support endpoint queries all "Stuck" OPACH 835 transactions that are waiting on bank account info back from Optum, then
        /// re publishes them to the /api/Edi835Support/PublishTransactions endpoint (batching if necessary)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("PokeSlowOpach")]
        [Produces("application/json", "application/text")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        public async Task<ActionResult> PokeSlowOpach(CancellationToken cancellationToken = default)
        {
            IList<long> stuckOpachTransactionIds = await _edi835Repository.GetStuckOpachTransactions(cancellationToken);

            _logger.LogInformation($"Number of stuck OPACH transactions: {stuckOpachTransactionIds.Count}");

            IList<PrfTransaction> prfTransactions = await _prfTransactionRepository.GetPrfTransactionsByTransactionIds(stuckOpachTransactionIds, cancellationToken);

            // Only PrfTransactions that have Is835Recieved are ready for republishing
            IList<long> filteredTransactions = prfTransactions.Where(txn => txn.Is835Received).Select(txn => txn.TransactionId).ToList();

            _logger.LogInformation($"Number of stuck OPACH transactions to be published: {filteredTransactions.Count}");

            var resultSet = new Dictionary<long, string>();
            // Batch the txns into every "maxTransactions" amount of txn Ids
            for (int i = 0; i < filteredTransactions.Count; i += maxTransactions)
            {
                var chunkedList = filteredTransactions.Skip(i).Take(maxTransactions).ToList();

                foreach (var publishTransactionResult in await _edi835GenerationMessagePublisher.PublishTransactions(chunkedList, _user, _programName, cancellationToken))
                {
                    resultSet.Add(publishTransactionResult.Key, publishTransactionResult.Value);
                }
            }

            return Accepted(resultSet);                   
        }

        


        #endregion

        /// <summary>
        /// Clears the Edi835Configuration Cache
        /// </summary>
        /// <returns></returns>
        [HttpPost("ClearCache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ClearCache()
        {
            _edi835ConfigurationCache.Clear();
            return Ok();
        }
    }
}
