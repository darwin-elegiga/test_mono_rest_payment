using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Application.About.Queries;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public sealed class AboutController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AboutController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        ///     About the build that created the service
        /// </summary>
        /// <returns>Information about the build that created the service</returns>
        /// <response code="200">Information about the build that created the service</response>
        [HttpGet]
        [ProducesResponseType(typeof(GetAboutInfo.AboutInfo), StatusCodes.Status200OK)]
        public async Task<GetAboutInfo.AboutInfo> GetDetail(CancellationToken cancellationToken)
        {
            return await _mediator.Send(new GetAboutInfo.Query(), cancellationToken);
        }
    }
}