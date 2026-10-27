using System;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class ReminderFaxController : ControllerBase
    {
        private readonly IReminderFaxMessageQueuer _reminderFaxMessageQueuer;
        private readonly IReminderFaxService _reminderFaxService;
        private readonly ILogger<ReminderFaxController> _logger;

        public ReminderFaxController(
            IReminderFaxMessageQueuer reminderFaxMessageQueuer,
            IReminderFaxService reminderFaxService,
            ILogger<ReminderFaxController> logger)
        {
            ThrowIf.IsNull(reminderFaxMessageQueuer, nameof(reminderFaxMessageQueuer));
            ThrowIf.IsNull(reminderFaxService, nameof(reminderFaxService));
            ThrowIf.IsNull(logger, nameof(logger));

            _reminderFaxMessageQueuer = reminderFaxMessageQueuer;
            _reminderFaxService = reminderFaxService;
            _logger = logger;
        }

        /// <summary>
        /// Will process all pending reminder faxes for the given client type and date range, inclusively
        /// </summary>
        /// <param name="requestClientType">client type to process, defaults to <see cref="ClientType.BOTH"/></param>
        /// <param name="startDate">defaults to today, inclusive</param>
        /// <param name="endDate">defaults to today, inclusive</param>
        /// <returns></returns>
        [HttpPost]
        [Route("Queue")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> QueuePending(ClientType? requestClientType, DateTime? startDate, DateTime? endDate)
        {
            requestClientType = requestClientType ?? ClientType.BOTH;
            startDate = startDate ?? DateTime.Now;
            endDate = endDate ?? DateTime.Now;

            using (new PerfTimerLogger(_logger, "Reminder Fax Queuing"))
            using (_logger.BeginScope(new
            {
                RequestClientType = requestClientType.ToString(),
                StartDate = startDate?.Date,
                EndDate = endDate?.Date
            }.ToDictionary()))
            {
                await _reminderFaxMessageQueuer.QueueReminderFaxes(requestClientType.Value, startDate.Value.Date, endDate.Value.Date);

                return Accepted();
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Process(ReminderFaxRequest reminderFaxRequest)
        {
            await _reminderFaxService.ProcessReminderFaxRequest(reminderFaxRequest);

            return Accepted();
        }
    }
}
