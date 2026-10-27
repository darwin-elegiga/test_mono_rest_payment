using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Application.InboundPayments.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Queries;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Route("api/Inbound835Payment")]
    [ApiController]
    public class InboundPaymentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InboundPaymentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        ///     Gets inbound payment information
        /// </summary>
        /// <param name="payerId">Payer Id linked to the payment</param>
        /// <param name="paymentId">Payment Id/User Key on the payment</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        [HttpGet]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult<GetInboundPaymentWithClobByUserKeyPayer835IdSql.Result>> GetPaymentBatch([FromQuery, Required(AllowEmptyStrings = false)] string payerId, [FromQuery, Required(AllowEmptyStrings = false)] string paymentId, CancellationToken cancellationToken)
        {
            var query = new GetPaymentByPayerIdUserKey.Query { PayerId = payerId, UserKey = paymentId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return result;
        }

        /// <summary>
        ///     Gets the InboundPayment with the PaymentClob
        /// </summary>
        /// <param name="id">The id of the InboundPayment to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If the payment was found and returned.</response>
        [HttpGet("{id}/clob")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult<GetInboundPaymentWithClobByIdSql.Result>> GetInboundPayment(long id, CancellationToken cancellationToken)
        {
            var query = new GetPaymentByInboundPaymentId.Query { InboundPaymentId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return result;
        }

        /// <summary>
        ///     Gets the associated header for the Inbound Payment
        /// </summary>
        /// <param name="id">The id of the InboundPayment to retrieve the header for.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If the payment was found and returned.</response>
        [HttpGet("{id}/header")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult<InboundHeader>> GetInboundHeader(long id, CancellationToken cancellationToken)
        {
            var query = new GetHeaderByPaymentId.Query(id);
            var result = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return NotFound();
            }

            return result;
        }
    }
}
