using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Api.Controllers.Models;
using VPay.Optum.PaymentRouting.Application.Outbound.Commands;
using VPay.Optum.PaymentRouting.Application.Outbound.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class Outbound835Controller : ControllerBase
    {
        private readonly IMediator _mediator;

        public Outbound835Controller(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <summary>
        /// Verifies if 835 document exists or can be created and can be downloaded for the passed in Transaction Id.
        /// </summary>
        /// <param name="transactionId">The id of the 835 document to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If 835 data for the requested transaction id is found.</response>
        /// <response code="400">If the requested transaction id is invalid.</response>
        /// <response code="404">If 835 data for the requested transaction id is not found.</response>
        [HttpGet("transaction/{transactionId}/can-download")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult> CanDownloadOutbound835Document(long transactionId, CancellationToken cancellationToken)
        {
            if (transactionId <= 0)
            {
                return BadRequest($"Invalid transaction ID ({transactionId})");
            }

            Outbound835Data result = await _mediator.Send(new GetOutbound835FileDataByTransactionId.Query(transactionId), cancellationToken);

            if (result == null)
            {
                return NotFound($"835 Document not found for provided transaction ID ({transactionId})");
            }

            Outbound835ExistsDto dto = new() { TransactionId = transactionId, Exists = true };

            return Ok(dto);
        }

        /// <summary>
        /// Retrieves 835 document for the passed in Transaction Id
        /// </summary>
        /// <param name="transactionId">The id of the 835 document to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If 835 data with the id is found.</response>
        /// <response code="400">If 835 document cannot be created</response>
        /// <response code="404">If 835 data with the id is not found.</response>
        [HttpGet("transaction/{transactionId}/download")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult> DownloadOutbound835Document(long transactionId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOutbound835Document.Query(transactionId), cancellationToken);

            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound();
            }

            if (result.Status == ResultStatus.Error)
            {
                return BadRequest(result.Errors.ToString());
            }

            return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartOutbound835(CancellationToken cancellationToken)
        {
            var count = await _mediator.Send(new Queue835Files.Command(), cancellationToken);
            return Accepted(count);
        }
    }
}
