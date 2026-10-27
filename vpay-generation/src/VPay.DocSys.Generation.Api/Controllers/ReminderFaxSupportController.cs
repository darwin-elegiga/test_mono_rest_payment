using System;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.Data.Db2.Abstractions;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class ReminderFaxSupportController : ControllerBase
    {
        private readonly IReminderFaxPdfCreator _reminderFaxPdfCreator;
        private readonly IFax _faxRepository;
        private readonly IPlatformConfigurationApiClient _platformConfigurationApiClient;
        private readonly IParsingApiClient _parsingApiClient;
        private readonly ReminderFaxEmailSettings _reminderFaxEmailSettings;
        private readonly IGreenArrowApiClient _greenArrowApiClient;
        private readonly IPayorDetailResolver _payorDetailResolver;

        public ReminderFaxSupportController(
            IReminderFaxPdfCreator reminderFaxPdfCreator,
            IFax faxRepository,
            IPlatformConfigurationApiClient platformConfigurationApiClient,
            IParsingApiClient parsingApiClient,
            ReminderFaxEmailSettings reminderFaxEmailSettings,
            IGreenArrowApiClient greenArrowApiClient,
            IPayorDetailResolver payorDetailResolver)
        {
            _reminderFaxPdfCreator = reminderFaxPdfCreator;
            _faxRepository = faxRepository;
            _platformConfigurationApiClient = platformConfigurationApiClient;
            _parsingApiClient = parsingApiClient;
            _reminderFaxEmailSettings = reminderFaxEmailSettings;
            _greenArrowApiClient = greenArrowApiClient;
            _payorDetailResolver = payorDetailResolver;
        }

        /// <summary>
        /// Will create a sample cover page pdf for the given values in <see cref="CoverSheetParameters"/>
        /// </summary>
        /// <param name="request">values used in the cover page, no validation</param>
        /// <returns>a reminder fax cover page pdf</returns>
        [HttpPost("CoverPage")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateReminderFaxCoverPage(CoverSheetParameters request)
        {
            var result = await _reminderFaxPdfCreator.CreatePdfStream(request).ConfigureAwait(false);

            return File(result, "application/pdf", $"TEST_RFAX_Cover_sheet-{DateTime.Now:yyyymmddhhMMss}.pdf");
        }

        /// <summary>
        /// Will return all pending reminder faxes that need to be sent for the given date range
        /// </summary>
        /// <param name="clientType">what type of client, defaults to BOTH</param>
        /// <param name="startDate">defaults to today, inclusive</param>
        /// <param name="endDate">defaults to today, inclusive</param>
        /// <returns>all <see cref="ReminderFax"/>s for the given date range</returns>
        [HttpGet("PendingJobs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingJobs(ClientType? clientType, DateTime? startDate, DateTime? endDate)
        {
            clientType = clientType ?? ClientType.BOTH;
            startDate = startDate ?? DateTime.Now.Date;
            endDate = endDate ?? DateTime.Now.Date;

            var results = (await _faxRepository.GetReminderFaxes(clientType.Value, startDate.Value, endDate.Value).ConfigureAwait(false))
                .OrderBy(_ => _.ClientType)
                .ThenBy(_ => _.ReminderType)
                .ThenBy(_ => _.OriginCode)
                .ThenBy(_ => _.TPACode)
                .ThenBy(_ => _.BillingEntity)
                .ThenBy(_ => _.PaymentTypeCode)
                .ThenBy(_ => _.SeTransactionId)
                .GroupBy(_ => _.BillingEntity)
                .ToDictionary(
                    k => k.Key,
                    v => new
                    {
                        Count = v.Count(),
                        Values = v.ToList()
                    });

            return Ok(results);
        }

        /// <summary>
        /// Will send an email using <see cref="ReminderFaxEmailRequest"/>
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="toName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost("Email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Email(string toAddress, string toName = "TEST NAME", string message = "THIS IS A TEST")
        {
            var settings = _reminderFaxEmailSettings;

            settings.ToEmail = toAddress;
            settings.ToName = toName;
            settings.FromName = "TEST - " + settings.FromName;

            var request = new ReminderFaxEmailRequest(settings, DateTime.Now, DateTime.Now)
            {
                MessageText = message
            };

            await _greenArrowApiClient.SendEmail(request).ConfigureAwait(false);

            return Ok();
        }

        /// <summary>
        /// Will return <see cref="PayorDetail"/>
        /// </summary>
        /// <param name="tpa"></param>
        /// <param name="be"></param>
        /// <param name="clientType"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        [HttpGet("PayorDetail/{clientType}/{tpa}/{be}/{transactionId}")]
        [ProducesResponseType(typeof(PayorDetail), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayorDetail(ClientType clientType, string tpa, string be, long transactionId)
        {
            if (clientType == ClientType.BOTH)
            {
                return BadRequest($"{nameof(clientType)} cannot be {ClientType.BOTH}");
            }

            var otbDetail = await _platformConfigurationApiClient.GetOtbDetail(tpa, be).ConfigureAwait(false);

            var configuration = await _parsingApiClient.GetXmlConfiguration(tpa, be).ConfigureAwait(false);

            var fields = _payorDetailResolver.CreateFieldDictionary(otbDetail, configuration);

            var result = await _payorDetailResolver.ResolvePayorDetails(
                clientType,
                tpa,
                be,
                transactionId,
                otbDetail,
                fields).ConfigureAwait(false);

            return Ok(result);
        }
    }
}
