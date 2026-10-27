using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Application.Outbound.Commands;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.OutboundProcessor.Application.Commands;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class OutboundFinSegController : Controller
    {
        private readonly IMediator _mediator;
        public OutboundFinSegController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// If any payments are in <see cref="PaymentStatus.FinSegBuilt"/>, this will generate the files (an 835 and an associated FINSEG file).
        /// Once the files have been generated, they will be in <see cref="PaymentStatus.FinSegQueued"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("start")]
        public async Task<ActionResult> StartOutbound835(CancellationToken cancellationToken)
        {
            var count = await _mediator.Send(new QueueFinSegFiles.Command(), cancellationToken);
            return Accepted(count);
        }

        /// <summary>
        /// If the file was successfully sent to Optum, this can be used to queue up messages for
        /// each transaction in the file to sent the SENTOPTUM status to iSeries to be displayed
        /// in VPay 360
        /// </summary>
        /// <returns>Number of transaction sent to iSeries for SENTOPTUM</returns>
        [HttpPost("Support/SendCtrStatus/SentOptum/{outboundFileId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SendCtrStatus_SentOptum(
            long outboundFileId,
            CancellationToken token = default)
        {
            var count = await _mediator.Send(new CreateCtrStatusPublishCommand.Command(outboundFileId), token);
            return Accepted(count);
        }
    }
}
