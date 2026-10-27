using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VPay.Optum.Http.PreferencingApi.Models;
using VPay.Optum.Http.PreferencingApi.Queries;
using VPay.Optum.PaymentRouting.Application.Testing.Commands;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.BatchManifest;
using VPay.Optum.PaymentRouting.Data.Sql.BatchSummary;
using Q = VPay.Optum.PaymentRouting.Application.Testing.Queries;

namespace VPay.Optum.PaymentRouting.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class TestingController : Controller
    {
        private readonly ILogger<TestingController> _logger;
        private readonly IMediator _mediator;

        public TestingController(ILogger<TestingController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpPost("rabbit/publish")]
        public async Task<Unit> PublishRabbitMessage([FromQuery] string optumBatchId)
        {
            return await _mediator.Send(new PublishRabbitMessageTest.Command { OptumBatchId = optumBatchId },
                CancellationToken.None);
        }

        [HttpGet("BatchManifest")]
        public async Task<BatchManifest> GetBatchManifest([FromQuery] long InboundBatchId, CancellationToken cancellationToken)
        {
            return await _mediator.Send(new Q.GetBatchManifestById.Query(InboundBatchId),
                cancellationToken);
        }

        [HttpGet("BatchSummary")]
        public async Task<BatchSummary> GetBatchSummary([FromQuery] long InboundBatchId, CancellationToken cancellationToken)
        {
            return await _mediator.Send(new Q.GetBatchSummary.Query(InboundBatchId),
                cancellationToken);
        }

        [HttpPut("InboundBatch")]
        public async Task<BatchSummary> InsertInboundBatch([FromQuery] string OptumBatchId,
           [FromQuery] string Payer835Id,
           [FromQuery] int TotalPaymentCount,
           [FromQuery] decimal TotalPaymentAmount, CancellationToken cancellationToken)
        {
            return await _mediator.Send(new Q.InsertInboundBatch.Query(
                    OptumBatchId,
                    Payer835Id,
                    TotalPaymentCount,
                    TotalPaymentAmount
                ), cancellationToken);
        }

        [HttpPut("InboundHeader")]
        public async Task<BatchSummary> InsertInboundHeader(
            [FromQuery] string OptumBatchId,
            [FromQuery] string Payer835Id,
            [FromQuery] string PayerSubpayerId,
            [FromQuery] string FileProcId,
            [FromQuery] string PayerAlias,
            [FromQuery] string PaymentClob, CancellationToken cancellationToken)
        {
            return await _mediator.Send(
                new Q.InsertInboundHeader.Query(
                    OptumBatchId,
                    Payer835Id,
                    PayerSubpayerId,
                    FileProcId,
                    PayerAlias,
                    PaymentClob
                ), cancellationToken);
        }

        [HttpPut("InboundPayment")]
        public async Task<BatchSummary> InsertInboundPayment(
           [FromQuery] string OptumBatchId,
           [FromQuery] string Payer835Id,
           [FromQuery] string PayerSubpayerId,
           [FromQuery] string FileProcId,
           [FromQuery] string PayerAlias,
           [FromQuery] string ProvTIN,
           [FromQuery] string ZipCode,
           [FromQuery] string UserKey,
           [FromQuery] decimal PaymentAmount,
           [FromQuery] string PaymentClob, CancellationToken cancellationToken)
        {
            return await _mediator.Send(
                new Q.InsertInboundPayment.Query(
                    OptumBatchId,
                    Payer835Id,
                    PayerSubpayerId,
                    FileProcId,
                    PayerAlias,
                    ProvTIN,
                    ZipCode,
                    UserKey,
                    PaymentAmount,
                    PaymentClob
                ), cancellationToken);
        }

        [HttpPost("InboundBatchStatus")]
        public async Task<int> InsertStatus([FromQuery] long batchId)
        {
            BatchStatus status;
            switch (new Random().Next(4))
            {
                case 0:
                    status = BatchStatus.BatchInitiated;
                    break;

                case 1:
                    status = BatchStatus.BatchReceived;
                    break;

                case 2:
                    status = BatchStatus.BatchComplete;
                    break;

                default:
                    status = BatchStatus.BatchCancelled;
                    break;
            }

            return await _mediator.Send(new InsertBatchStatus.Command
            {
                InboundBatchId = batchId,
                Status = status.Name
            });
        }

        /// <summary>
        /// Pass-thru testing for Preferencing Optum ACH Provider Enrollment Solve
        /// </summary>
        [HttpGet("OptumAchProviderEnrollmentSearch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<OptumAchEnrollment>> OptumAchProviderEnrollmentSearch(
            string clientCode,
            string taxId,
            CancellationToken token = default)
        {
            var result = await _mediator.Send(new OptumAchEnrollmentSearchQuery(
                new OptumAchEnrollmentSearchParam( clientCode, taxId )
                 ), token);
            return Ok(result);
        }

    }
}
