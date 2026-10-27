using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Api.Controllers.Models;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Commands;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Queries;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InboundBatchController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InboundBatchController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        ///     Retrieves an Optum batch based on the passed in id.
        /// </summary>
        /// <param name="id">The id of the batch to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a record with the id is found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If a record with the id is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult<GetInboundBatchResult>> GetBatchWithPayments(long id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBatchWithPayments.Query {Id = id}, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }

            return new GetInboundBatchResult
            {
                Status = result.Status.Name,
                BatchCreatedDateTime = result.BatchCreatedDateTime,
                HeaderCreatedDateTime = result.HeaderCreatedDateTime,
                HeaderData = result.HeaderData,
                InboundBatchId = result.InboundBatchId,
                Payer835Id = result.Payer835Id,
                PayerAlias = result.PayerAlias,
                PayerSubpayerId = result.PayerSubpayerId,
                OptumBatchId = result.OptumBatchId,
                TotalPaymentCount = result.TotalPaymentCount,
                TotalPaymentAmount = result.TotalPaymentAmount,
                InboundPaymentIds = result.InboundPaymentIds,
                InboundHeaderId = result.InboundHeaderId
            };
        }

        /// <summary>
        ///     Marks an Inbound Optum batch as completed.
        /// </summary>
        /// <param name="id">The id of the batch to mark complete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a batch status was successfully set to completed.</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(Status200OK)]
        public async Task<ActionResult> CompleteBatch(long id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CompleteInboundBatch.Command { InboundBatchId = id }, cancellationToken);
            return Ok();
        }
    }
}
