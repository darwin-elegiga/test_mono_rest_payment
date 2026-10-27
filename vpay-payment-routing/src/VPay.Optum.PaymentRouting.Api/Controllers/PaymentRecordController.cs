using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Queries;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentRecordController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentRecordController(IMediator mediator) => _mediator = mediator;

        /// <summary>
        ///     Retrieves an Optum payment record based on the passed in id.
        /// </summary>
        /// <param name="id">The id of the payment record to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If a record with the id is found.</response>
        /// <response code="400">If the parameters are invalid.</response>
        /// <response code="404">If a record with the id is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult<GetPaymentRecord.Result>> GetPaymentRecord(long id, CancellationToken cancellationToken)
        {
            GetPaymentRecord.Result result = await _mediator.Send(new GetPaymentRecord.Query {Id = id}, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }

            return result;
        }
    }
}
