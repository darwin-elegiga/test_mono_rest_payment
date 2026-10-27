using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.Queuing;

namespace VPay.DocSys.Parsing.Api.Controllers
{
    [Route("/api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class Edi835SupportController : ControllerBase
    {
        private readonly IStatusFilePathHelper _statusFilePathHelper;
        private readonly IEdi835TarService _edi835TarService;
        private readonly IEdi835Repository _edi835Repository;
        private readonly Edi835GenerationQueueSettings _edi835GenerationQueueSettings;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;
        private readonly IInbound835FileService _inbound835FileService;
        private readonly ILoggerFactory _loggerFactory;

        public Edi835SupportController(
            IStatusFilePathHelper statusFilePathHelper,
            IEdi835TarService edi835TarService,
            IEdi835Repository edi835Repository,
            Edi835GenerationQueueSettings edi835GenerationQueueSettings,
            Edi835ConfigurationCache edi835ConfigurationCache,
            IInbound835FileService inbound835FileService,
            ILoggerFactory loggerFactory
            )
        {
            _statusFilePathHelper = statusFilePathHelper ?? throw new ArgumentNullException(nameof(statusFilePathHelper));
            _edi835TarService = edi835TarService ?? throw new ArgumentNullException(nameof(edi835TarService));
            _edi835Repository = edi835Repository ?? throw new ArgumentNullException(nameof(edi835Repository));
            _edi835GenerationQueueSettings = edi835GenerationQueueSettings ?? throw new ArgumentNullException(nameof(edi835GenerationQueueSettings));
            _edi835ConfigurationCache = edi835ConfigurationCache ?? throw new ArgumentNullException(nameof(edi835ConfigurationCache));
            _inbound835FileService = inbound835FileService ?? throw new ArgumentNullException(nameof(inbound835FileService));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }



        /// <summary>
        /// If a status file has not been parsed and placed in the DB, will
        /// parse it, and if needed, will extract the .docs files to their expected location.
        /// </summary>
        [HttpPost("ParseStsFile")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<Edi835>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Edi835ParseStsFile(Edi835ParseStsFileRequest request, CancellationToken cancellationToken)
        {
            var validStsPath = _statusFilePathHelper.CleanAndValidateStsPath(request.StsFilepath);
            if (validStsPath is null)
            {
                return BadRequest("Invalid StsFilePath");
            }

            if (!System.IO.File.Exists(validStsPath))
            {
                return NotFound(request.StsFilepath);
            }
            var docTranMsgs = _edi835TarService.ParseSts(validStsPath);
            var edi835s = await _edi835TarService.ExtractDocs(docTranMsgs, cancellationToken);

            if (request.QueueForGeneration)
            {

                var publisher = QuorumPublisherFactory.GetPublisher(_edi835GenerationQueueSettings, _loggerFactory.CreateLogger<IMessagePublisher>(), DeliveryConfirmationMethod.Asynchronous);

                foreach (var edi in edi835s)
                {
                    if (!edi.IsTerminalStatus && edi.StatusCode != Edi835StatusCode.Initiated)
                    {
                        await CancelAndRestartEdi835(edi, cancellationToken);
                    }
                    await _edi835Repository.Add835Status(
                            edi.Edi835Id,
                            Edi835StatusCode.QueuedForGeneration,
                            "Queued from API",
                            "",
                            GetType().FullName,
                            User.Identity.Name,
                            cancellationToken
                        );
                    var wrappedMessage = JsonConvert.SerializeObject(new MessageWrapper<Edi835>(edi));
                    publisher.Publish(wrappedMessage);
                }

            }
            return Ok(edi835s);
        }

        private async Task CancelAndRestartEdi835(Edi835 edi, CancellationToken cancellationToken)
        {
            await _edi835Repository.Add835Status(
               edi.Edi835Id,
               Edi835StatusCode.CancelledByUser,
               "Cancelled by queuing new request",
               "",
               GetType().FullName,
               User.Identity.Name,
               cancellationToken
            );
            await _edi835Repository.Add835Status(
                edi.Edi835Id,
                Edi835StatusCode.Initiated,
                "Initiating Edi835 Request",
                "",
                GetType().FullName,
                User.Identity.Name,
                cancellationToken
             );
        }

        /// <summary>
        /// If a status file has not been parsed and placed in the DB, will
        /// parse it, and if needed, will extract the .docs files to their expected location.
        /// </summary>
        [HttpGet("GetAllEdi835sForStsFile")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<Edi835>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllEdi835sForStsFile(string stsFilePath, CancellationToken cancellationToken)
        {
            var validStsPath = _statusFilePathHelper.CleanAndValidateStsPath(stsFilePath);
            if (validStsPath is null)
            {
                return BadRequest("Invalid StsFilePath");
            }

            if (!System.IO.File.Exists(validStsPath))
            {
                return NotFound(stsFilePath);
            }
            var docTranMsgs = _edi835TarService.ParseSts(validStsPath);
            var transactionNeeding835 = docTranMsgs.Where(t => t.IsEdi835Needed(_edi835ConfigurationCache)).Select(t => Convert.ToInt64(t.TransactionId));
            var edi835s = await _edi835Repository.GetMostRecent835s(transactionNeeding835, cancellationToken);
            return Ok(edi835s);
        }


        /// <summary>
        /// If a status file has not been parsed and placed in the DB, will
        /// parse it, and if needed, will extract the .docs files to their expected location.
        /// </summary>
        [HttpGet("GetAllEdi835sForStsFileWithMessageWrapper")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(IList<MessageWrapper<Edi835>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllEdi835sForStsFileWithMessageWrapper(string stsFilePath, CancellationToken cancellationToken)
        {
            var validStsPath = _statusFilePathHelper.CleanAndValidateStsPath(stsFilePath);
            if (validStsPath is null)
            {
                return BadRequest("Invalid StsFilePath");
            }

            if (!System.IO.File.Exists(validStsPath))
            {
                return NotFound(stsFilePath);
            }

            var docTranMsgs = _edi835TarService.ParseSts(validStsPath);
            var transactionNeeding835 = docTranMsgs.Where(t => t.IsEdi835Needed(_edi835ConfigurationCache)).Select(t => Convert.ToInt64(t.TransactionId));
            var edi835s = await _edi835Repository.GetMostRecent835s(transactionNeeding835, cancellationToken);
            var messageWrapped = edi835s.Select(_ =>
            new MessageWrapper<Edi835>(_)).ToList();
            return Ok(messageWrapped);
        }

        /// <summary>
        /// Sends the 835 document for each of the specified transactions to Availity.
        /// </summary>
        /// <param name="request">An object that contains the list of transaction ids.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A collection of <see cref="TransactionOperationResponse" /> objects that contain the results of the operation for each transaction specified.
        /// </returns>
        [HttpPost("Send835sToAvaility")]
        [Produces("application/json", "application/text")]
        [ProducesResponseType(typeof(IList<TransactionOperationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostSend835sToAvaility(TransactionOperationRequest request, CancellationToken cancellationToken)
        {
            var results = new List<TransactionOperationResponse>(request.TransactionIds.Count);

            foreach (var transactionId in request.TransactionIds)
            {
                try
                {
                    await _inbound835FileService.Send835ToAvaility(transactionId, cancellationToken);

                    results.Add(new TransactionOperationResponse()
                    {
                        TransactionId = transactionId,
                        Result = "Success",
                    });
                }
                catch (Exception exception)
                {
                    results.Add(new TransactionOperationResponse()
                    {
                        TransactionId = transactionId,
                        Result = $"ERROR: {exception.Message}",
                    });
                }
            }

            return Ok(results);
        }

        /// <summary>
        /// Sends the BRN document for each of the specified transactions to Availity.
        /// </summary>
        /// <param name="request">An object that contains the list of transaction ids.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A collection of <see cref="TransactionOperationResponse" /> objects that contain the results of the operation for each transaction specified.
        /// </returns>
        [HttpPost("SendBRNsToAvaility")]
        [Produces("application/json", "application/text")]
        [ProducesResponseType(typeof(IList<TransactionOperationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostSendBRNsToAvaility(TransactionOperationRequest request, CancellationToken cancellationToken)
        {
            var results = new List<TransactionOperationResponse>(request.TransactionIds.Count);

            foreach (var transactionId in request.TransactionIds)
            {
                try
                {
                    await _inbound835FileService.SendBrnToAvaility(transactionId, cancellationToken);

                    results.Add(new TransactionOperationResponse()
                    {
                        TransactionId = transactionId,
                        Result = "Success",
                    });
                }
                catch (Exception exception)
                {
                    results.Add(new TransactionOperationResponse()
                    {
                        TransactionId = transactionId,
                        Result = $"ERROR: {exception.Message}",
                    });
                }
            }

            return Ok(results);
        }

        /// <summary>
        /// Gets the Edi 835 config for the clientCode
        /// </summary>
        /// <param name="clientCode">The TPA code for the client. EX: SWK, HEQTN, RC</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The name of the parser</returns>
        [HttpGet("GetEdi835Configuration")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEdi835Configuration(string clientCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientCode))
            {
                return BadRequest("clientCode must be provided");
            }

            try
            {
                var edi835Configurations = await _edi835Repository.GetEdi835Configurations(cancellationToken);
                var match = edi835Configurations.FirstOrDefault(x => x.ClientCode.Equals(clientCode, StringComparison.InvariantCultureIgnoreCase));
                if (match != null)
                {
                    return Ok(match.Parser);
                }
                else
                {
                    return NotFound();
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }

        /// <summary>
        /// Inserts the 835 config into the database
        /// </summary>
        /// <param name="clientCode">The TPA code for the client. EX: SWK, HEQTN, RC</param>
        /// <param name="parser">The following are supported: SWK, HEQTN, DirectSpec, DualSpec, RC</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("InsertEdi835Configuration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InsertEdi835Configuration(string clientCode, string parser, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientCode) || string.IsNullOrWhiteSpace(parser))
            {
                return BadRequest("clientCode and parser must be provided");
            }

            var validParsers = new List<string>
            {
                Edi835Parser.Swk,
                Edi835Parser.Heqtn,
                Edi835Parser.DirectSpec,
                Edi835Parser.DualSpec,
                Edi835Parser.RedCard
            };
            if (!validParsers.Contains(parser))
            {
                return BadRequest("Parser selection is invalid");
            }

            try
            {
                await _edi835Repository.InsertEdi835Configuration(clientCode, parser, cancellationToken);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }

            return Ok();
        }

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
