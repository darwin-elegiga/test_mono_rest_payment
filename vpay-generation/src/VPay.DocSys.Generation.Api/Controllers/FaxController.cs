using System;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.DocSys.Storage;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersionNeutral]
    [Produces("application/json")]
    [Route("api/fax")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class FaxController : ControllerBase
    {
        private readonly QueueConfigurations _queueConfigurations;
        private readonly IQueueService _queueService;
        private readonly IFaxArchiveMessageProcessor _faxArchiveMessageProcessor;
        private readonly ILogger<FaxController> _logger;

        public FaxController(
            QueueConfigurations queueConfigurations,
            IQueueService queueService,
            IFaxArchiveMessageProcessor faxArchiveMessageProcessor,
            ILogger<FaxController> logger)
        {
            ThrowIf.IsNull(queueConfigurations, nameof(queueConfigurations));
            ThrowIf.IsNull(queueConfigurations.FaxRetryQueue, nameof(queueConfigurations.FaxRetryQueue));
            ThrowIf.IsNull(queueService, nameof(queueService));
            ThrowIf.IsNull(faxArchiveMessageProcessor, nameof(faxArchiveMessageProcessor));
            ThrowIf.IsNull(logger, nameof(logger));

            _queueConfigurations = queueConfigurations;
            _queueService = queueService;
            _faxArchiveMessageProcessor = faxArchiveMessageProcessor;
            _logger = logger;
        }

        /// <summary>
        /// Will create a <see cref="FaxArchiveFile"/> using the <see cref="FaxRetryArchiveMessage"/>
        /// </summary>
        /// <param name="faxRetryArchiveMessage">a stripped down version of the <code>FaxJob</code> from the Fax Management solution, which can also be used directly</param>
        /// <returns></returns>
        [HttpPost("Archive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Archive(FaxRetryArchiveMessage faxRetryArchiveMessage)
        {
            using (GetPayloadLogScope(faxRetryArchiveMessage))
            {
                _logger.LogInformation("Process Fax Retry Archive Message");

                await _faxArchiveMessageProcessor.ProcessMessage(faxRetryArchiveMessage);

                return Ok();
            }
        }

        /// <summary>
        /// Will queue to a message to <see cref="QueueConfigurations.FaxRetryQueue"/> using the <see cref="FaxRetryArchiveMessage"/>
        /// </summary>
        /// <param name="faxRetryArchiveMessage">a stripped down version of the <code>FaxJob</code> from the Fax Management solution, which can also be used directly</param>
        /// <returns></returns>
        [HttpPost("QueueArchive")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult QueueArchive(FaxRetryArchiveMessage faxRetryArchiveMessage)
        {
            using (GetPayloadLogScope(faxRetryArchiveMessage))
            {
                _logger.LogInformation("Queue Fax Retry Archive Message");

                _queueService.PublishQueueMessage(
                    _queueConfigurations.FaxRetryQueue,
                    JsonConvert.SerializeObject(faxRetryArchiveMessage));

                return Accepted();
            }
        }

        private IDisposable GetPayloadLogScope(FaxRetryArchiveMessage faxRetryArchiveMessage)
        {
            var logScopeDictionary = faxRetryArchiveMessage.ToDictionaryExcept(nameof(FaxRetryArchiveMessage.TransactionIds));
            logScopeDictionary.Add("TransactionIds", string.Join(", ", faxRetryArchiveMessage.TransactionIds));

            return _logger.BeginScope(logScopeDictionary);
        }
    }
}
