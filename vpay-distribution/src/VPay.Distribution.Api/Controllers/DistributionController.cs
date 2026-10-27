using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Models;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.Queuing;

namespace VPay.Distribution.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistributionController : ControllerBase
    {
        private readonly IDistributionStatusService _distributionStatusService;
        private readonly ISshOperations _sshOperations;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly IEventMessagePublisher _eventMessagePublisher;
        private readonly FaxDistributionQueueConfigurationSettings _faxQueueSettings;
        private readonly MailDistributionQueueConfigurationSettings _mailQueueSettings;
        private readonly SendBackDistributionQueueConfigurationSettings _sendBackQueueSettings;

        public DistributionController(
            IDistributionStatusService distributionStatusService,
            ISshOperations sshOperations,
            IMessagePublisherService messagePublisherService,
            IEventMessagePublisher eventMessagePublisher,
            FaxDistributionQueueConfigurationSettings faxQueueSettings,
            MailDistributionQueueConfigurationSettings mailQueueSettings,
            SendBackDistributionQueueConfigurationSettings sendBackQueueSettings)
        {
            ArgumentHelpers.ThrowIf.IsNull(distributionStatusService, nameof(distributionStatusService));
            ArgumentHelpers.ThrowIf.IsNull(sshOperations, nameof(sshOperations));
            ArgumentHelpers.ThrowIf.IsNull(messagePublisherService, nameof(messagePublisherService));
            ArgumentHelpers.ThrowIf.IsNull(eventMessagePublisher, nameof(eventMessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(faxQueueSettings, nameof(faxQueueSettings));
            ArgumentHelpers.ThrowIf.IsNull(mailQueueSettings, nameof(mailQueueSettings));
            ArgumentHelpers.ThrowIf.IsNull(sendBackQueueSettings, nameof(sendBackQueueSettings));

            _distributionStatusService = distributionStatusService;
            _sshOperations = sshOperations;
            _messagePublisherService = messagePublisherService;
            _eventMessagePublisher = eventMessagePublisher;
            _faxQueueSettings = faxQueueSettings;
            _mailQueueSettings = mailQueueSettings;
            _sendBackQueueSettings = sendBackQueueSettings;
        }

        /// <summary>
        /// This will queue a message to fax all documents for all the <see cref="DocDistMsg.Transactions"/> in the <paramref name="entity"/>
        /// </summary>
        /// <remarks>
        /// Nothing returned
        /// </remarks>
        /// <param name="entity">This is the information needed to distribute documents to fax</param>
        /// <returns><see cref="StatusCodes.Status202Accepted"/></returns>
        [HttpPost("DistributeFaxes")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult DistributeFaxes(DocDistMsg entity)
        {
            string message = JsonConvert.SerializeObject(entity);

            _messagePublisherService.PublishMessage(_faxQueueSettings, message);

            return Accepted();
        }

        /// <summary>
        /// This will queue a message to mail all documents for all the <see cref="DocDistMsg.Transactions"/> in the <paramref name="entity"/>
        /// </summary>
        /// <remarks>
        /// Nothing returned
        /// </remarks>
        /// <param name="entity">This is the information needed to distribute documents to mail</param>
        /// <returns><see cref="StatusCodes.Status202Accepted"/></returns>
        [HttpPost("DistributeMail")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult DistributeMail(DocDistMsg entity)
        {
            string message = JsonConvert.SerializeObject(entity);

            _messagePublisherService.PublishMessage(_mailQueueSettings, message);

            return Accepted();
        }

        /// <summary>
        /// This will queue a message to generate a pdf and copy it to the network location where custom send back checks are processed for all the <see cref="DocDistMsg.Transactions"/> in the <paramref name="entity"/>
        /// </summary>
        /// <remarks>
        /// Nothing returned
        /// </remarks>
        /// <param name="entity">This is the information needed to generate the custom send back pdf to be copied.</param>
        /// <returns><see cref="StatusCodes.Status202Accepted"/></returns>
        [HttpPost("DistributeCustomSendBacks")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult DistributeCustomSendBacks(DocDistMsg entity)
        {
            string message = JsonConvert.SerializeObject(entity);

            _messagePublisherService.PublishMessage(_sendBackQueueSettings, message);

            return Accepted();
        }

        /// <summary>
        /// This will queue a message to publish events on all documents for all the <see cref="DocDistMsg.Transactions"/> in the <paramref name="entity"/>
        /// </summary>
        /// <remarks>
        /// Nothing returned
        /// </remarks>
        /// <param name="entity">This is the information needed to publish events</param>
        /// <returns><see cref="StatusCodes.Status202Accepted"/></returns>
        [HttpPost("DistributeEvents")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult DistributeEvents(DocDistMsg entity)
        {
            _eventMessagePublisher.PublishEvents(entity);

            return Accepted();
        }

        /// <summary>
        /// This will transfer all documents to the server, and will also create the control files given, afterwards
        /// </summary>
        /// <param name="serverParams">This is the information needed to distribute documents to the server</param>
        /// <returns>Returns a list of files that  have been created on the <paramref name="serverParams"/>'s Host</returns>
        [HttpPost("DistributeServer")]
        [ProducesResponseType(typeof(List<ServerFile>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DistributeServer(ServerParams serverParams)
        {
            ArgumentHelpers.ThrowIf.IsNull(serverParams, nameof(serverParams));
            ArgumentHelpers.ThrowIf.IsNull(serverParams.ScpSettings, nameof(serverParams.ScpSettings));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(serverParams.FtpFiles, nameof(serverParams.FtpFiles));

            foreach (var filePath in serverParams.FtpFiles.Select(_ => _.FilePath))
            {
                ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(filePath, nameof(filePath));
                await _sshOperations.TransferFileAsync(filePath, serverParams.ScpSettings);
            }

            if (serverParams.CreateCtrlFiles)
            {
                foreach (var controlFile in serverParams.FtpFiles.Select(_ => _.ControlFileName))
                {
                    ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(controlFile, nameof(controlFile));
                    await _sshOperations.TouchControlFileOnServerAsync(controlFile, serverParams.ScpSettings);
                }
            }

            return Ok(serverParams.FtpFiles);
        }

        /// <summary>
        /// Sets the distribution status to complete for the specified status file.
        /// </summary>
        /// <param name="statusFilePath">Full path to STS</param>
        /// <param name="distType">M or F for Mail or Fax distribution or empty for all.</param>
        /// <returns><see cref="StatusCodes.Status202Accepted"/></returns>
        [HttpPost("DistributionComplete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DistributionComplete(string statusFilePath, string distType)
        {
            if (string.IsNullOrEmpty(statusFilePath))
            {
                return new BadRequestResult();
            }

            await _distributionStatusService.SetDistributionStatusComplete(statusFilePath, distType);

            return Ok();
        }

        /// <summary>
        /// Deletes the distribution status for the specified status file.
        /// </summary>
        /// <param name="statusFilePath">Full path to STS (filename/extension included)</param>
        /// <param name="distType">M or F for Mail or Fax distribution or empty for all.</param>
        /// <returns><see cref="StatusCodes.Status200OK"/></returns>
        [HttpPost("ClearDistributionStatus")]
        [ProducesResponseType(typeof(ClearDistributionStatusResults), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ClearDistributionStatusResults), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClearDistributionStatus(string statusFilePath, string distType)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(statusFilePath, nameof(statusFilePath));

            DistributionMethod distMethod;
            switch (distType ?? string.Empty)
            {
                case "M":
                    distMethod = DistributionMethod.Mail;
                    break;
                case "F":
                    distMethod = DistributionMethod.Fax;
                    break;
                case "":
                    distMethod = DistributionMethod.None;
                    break;
                default:
                    return BadRequest();
            }

            var results = await _distributionStatusService.ClearDistributionStatus(statusFilePath, distMethod);

            if (results.IsSuccess(distMethod))
            {
                return Ok(results);
            }

            return NotFound(results);
        }
    }
}
