using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;
using VPay.Queuing;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.Distribution.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class SupportController : ControllerBase
    {
        private readonly IEventMessagePublisher _eventService;
        private readonly IEdi835MessagePublisher _edi835MessagePublisher;
        private readonly ILogger<SupportController> _logger;

        public SupportController(
            IEventMessagePublisher eventMessagePublisher,
            IEdi835MessagePublisher edi835MessagePublisher,
            ILogger<SupportController> logger)
        {
            ThrowIf.IsNull(eventMessagePublisher, nameof(eventMessagePublisher));
            ThrowIf.IsNull(edi835MessagePublisher, nameof(edi835MessagePublisher));
            ThrowIf.IsNull(logger, nameof(logger));

            _eventService = eventMessagePublisher;
            _edi835MessagePublisher = edi835MessagePublisher;
            _logger = logger;
        }

        /// <summary>
        /// Will Publish events for all files for each <see cref="DistTxnData"/> within <see cref="DocDistMsg.Transactions"/> for <paramref name="request"/>
        /// </summary>
        /// <remarks>
        /// Will validate each transactions for a final Distribution state before publishing events
        /// </remarks>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        [HttpPost("QueryAndPublishEventsByTransactionIds")]
        [ProducesResponseType(typeof(EventPublishResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(EventPublishResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(EventPublishResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> QueryAndPublishEventsByTransactionIds(DocDistMsg request, CancellationToken cancellationToken = default)
        {
            using (new PerfTimerLogger(_logger, "Support - Query and Publish Events By Transaction Ids"))
            using (_logger.BeginScope(new
            {
                StatusFileId = request.PaymentStatusFileId.ToString().Replace(Environment.NewLine, ""),
                OriginCode = request.Origin.Replace(Environment.NewLine, ""),
                BillingEntityCode = request.BillingEntity.Replace(Environment.NewLine, ""),
                ClientCode = request.TPA.Replace(Environment.NewLine, ""),
                NextWarningTime = request.WarningTime.ToString().Replace(Environment.NewLine, ""),
                Transactions = request.Transactions?.Count.ToString().Replace(Environment.NewLine, ""),
                IsRedCard = request.IsRedCard.ToString().Replace(Environment.NewLine, "")
            }.ToDictionary()))
            {
                try
                {
                    var response = await _eventService.ValidateAndPublishEventsForTransactionIds(request, cancellationToken);

                    var hasNotDist = response.TransactionsNotDistributed.Count > 0;
                    var hasNoDistStatus = response.TransactionsWithoutDistributionStatus.Count > 0;

                    if (hasNotDist)
                    {
                        response.Notes += "  Some transactions are not ready for distribution.";
                    }

                    if (hasNoDistStatus)
                    {
                        response.Notes += "  Some transactions have no final distribution status.";
                    }

                    if (response.TransactionsWithEventsPublished.Count == 0)
                    {
                        response.Notes = "No transactions sent for distribution events." + response.Notes;

                        if (hasNotDist)
                        {
                            return Conflict(response);
                        }
                        else if (hasNoDistStatus)
                        {
                            return NotFound(response);
                        }
                    }

                    return Accepted(response);
                }
                catch (Exception e) when (_logger.LogException(e))
                {
                    return BadRequest(e);
                }
            }
        }

        /// <summary>
        /// Will Publish events for all files for each <see cref="DistTxnData"/> within <see cref="DocDistMsg.Transactions"/> for <paramref name="request"/>
        /// </summary>
        /// <remarks>
        /// Will not validate state of transactions
        /// </remarks>
        /// <param name="request"></param>
        [HttpPost("PublishEventsByTransactionIdsWithoutValidation")]
        [ProducesResponseType(typeof(EventPublishResponse), StatusCodes.Status202Accepted)]
        public IActionResult PublishEventsByTransactionIdsWithoutValidation(DocDistMsg request)
        {
            using (new PerfTimerLogger(_logger, "Support - Publish Events By Transaction Ids Without Validation"))
            using (_logger.BeginScope(new
            {
                StatusFileId = request.PaymentStatusFileId.ToString().Replace(Environment.NewLine, ""),
                OriginCode = request.Origin.Replace(Environment.NewLine, ""),
                BillingEntityCode = request.BillingEntity.Replace(Environment.NewLine, ""),
                ClientCode = request.TPA.Replace(Environment.NewLine, ""),
                NextWarningTime = request.WarningTime.ToString().Replace(Environment.NewLine, ""),
                Transactions = request.Transactions?.Count.ToString().Replace(Environment.NewLine, ""),
                IsRedCard = request.IsRedCard.ToString().Replace(Environment.NewLine, "")
            }.ToDictionary()))
            {
                try
                {
                    _eventService.PublishEvents(request);
                }
                catch (Exception e) when (_logger.LogException(e))
                {
                    return BadRequest(e);
                }

                return Accepted();
            }
        }

        /// <summary>
        /// This will queue a message to publish Edi835 generation on all documents with the IsEdi835Needed flag for all the <see cref="DocDistMsg.Transactions"/> in the <paramref name="entity"/>
        /// </summary>
        /// <remarks>
        /// Nothing returned
        /// </remarks>
        /// <param name="entity">This is the information needed to publish Edi835CreationMessages</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="StatusCodes.Status202Accepted"/></returns>
        [HttpPost("DistributeEdi835")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> DistributeEdi835Async(DocDistMsg entity, CancellationToken cancellationToken)
        {
            var response = await _edi835MessagePublisher.PublishEdi835Messages(entity, cancellationToken);
            return Accepted(response);
        }

    }
}
