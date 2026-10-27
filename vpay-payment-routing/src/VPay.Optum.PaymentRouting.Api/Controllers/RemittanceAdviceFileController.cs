using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Sql.RemittanceAdviceFile.Commands;
using static Microsoft.AspNetCore.Http.StatusCodes;
using MT = MassTransit;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Route("api/[controller]")]
    [Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Consumes(System.Net.Mime.MediaTypeNames.Application.Json)]
    [ApiController]
    public class RemittanceAdviceFileController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IPublishEndpoint _publishEndpoint;

        public RemittanceAdviceFileController(IMediator mediator, MT.IPublishEndpoint publishEndpoint)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        /// <summary>
        /// Inserts a record into the dbo.RemittanceAdviceFile table
        /// </summary>
        /// <param name="dto">Details of the record to insert</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Id of the record inserted</returns>
        [HttpPost]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), Status409Conflict)]
        public async Task<IActionResult> InsertRemittanceAdviceFile(
            RemittanceAdviceFileMessage dto,
            CancellationToken token = default)
        {
            var command = new InsertRemittanceAdviceFile.Command(
                dto.Payer835Id,
                dto.InboundZipfile,
                dto.UserKey,
                dto.PayerAlias,
                dto.OriginCode,
                dto.ClientCode,
                dto.BillingEntityCode,
                dto.RemitMethod,
                dto.PdfExtractPath,
                dto.CreatedBy
            );
            try
            {
                var result = await _mediator.Send(command, token).ConfigureAwait(false);
                if (result.Errors.Contains("Duplicate"))
                {
                    return Conflict(new ProblemDetails
                    {
                        Status = Status409Conflict,
                        Title = "Sql Unique Key Violation",
                        Detail = $"Unable to insert duplicate record for payer835Id:{dto.Payer835Id}, inboundZipfile:{dto.InboundZipfile}, userKey:{dto.UserKey}",
                        Instance = HttpContext.Request.Path
                    });
                }
                // If there is an existing batch for this PDF, trigger a completion check.
                if (result.IsSuccess && result.Value?.InboundBatchId != null)
                {
                    await _publishEndpoint.Publish(new CheckBatchCompletionMessage(result.Value.InboundBatchId.Value), token).ConfigureAwait(false);
                }
                return Ok(result);
            }
            catch (InvalidOperationException ioex)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = Status400BadRequest,
                    Title = "Invalid Operation Exception",
                    Detail = ioex.Message,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = Status400BadRequest,
                    Title = "Unexpected Exception",
                    Detail = ex.Message,
                    Instance = HttpContext.Request.Path
                });
            }
        }


        /// <summary>
        /// Publix a record to queue to be inserting into the dbo.RemittanceAdviceFile table
        /// </summary>
        /// <param name="dto">Details of the record to insert</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Id of the record inserted</returns>
        [HttpPost("QueueRemittanceAdviceFile")]
        [ProducesResponseType(Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), Status400BadRequest)]
        public async Task<IActionResult> QueueRemittanceAdviceFile(
            RemittanceAdviceFileMessage dto,
            CancellationToken token = default)
        {
            try
            {
                await _publishEndpoint.Publish(dto, token).ConfigureAwait(false);
                return Accepted();
            }
            catch (InvalidOperationException ioex)
            {
                return BadRequest(
                    new ProblemDetails
                    {
                        Status = Status400BadRequest,
                        Title = "Invalid Operation Exception",
                        Detail = ioex.Message,
                        Instance = HttpContext.Request.Path
                    });
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new ProblemDetails
                    {
                        Status = Status400BadRequest,
                        Title = "Unexpected Exception",
                        Detail = ex.Message,
                        Instance = HttpContext.Request.Path
                    });
            }
        }
    }
}
