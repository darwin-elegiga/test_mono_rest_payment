using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Application.InboundHeader.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.InboundHeader;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class InboundHeaderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InboundHeaderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gets the InboundHeader by the Database Id.
        /// </summary>
        /// <param name="id">The id of the InboundHeader to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
        /// <response code="200">If the payment was found and returned.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<ActionResult<InboundHeader>> GetInboundHeader(long id, CancellationToken cancellationToken)
        {
            var query = new GetInboundHeaderByInboundHeaderId.Query { InboundHeaderId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}
