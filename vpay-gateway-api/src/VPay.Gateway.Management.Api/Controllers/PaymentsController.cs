using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Gateway.Management.Api.Models;
using VPay.Gateway.Management.Application.GuidewirePayments;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Management.Api.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public PaymentsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// This gets statues for a Payment sent to Guidewire for the ClientCode/PaymentId
    /// </summary>
    /// <param name="clientCode"></param>
    /// <param name="clientPaymentId"></param>
    /// <param name="token"></param>
    /// <response code="400">If parameters are invalid.</response>
    /// <response code="404">Could not locate payment information</response>
    [HttpGet("clients/{clientCode}/sent-statuses")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<ActionResult<GuidewirePaymentStatusResult>> GetStatuses(
        [Required(AllowEmptyStrings = false), MaxLength(10)] string clientCode,
        [Required(AllowEmptyStrings = false), MaxLength(100)] string clientPaymentId,
        CancellationToken token)
    {
        var query = await _mediator.Send(new GetPaymentStatusTrackingQuery(clientCode, clientPaymentId), token).ConfigureAwait(false);

        if (query == null || query.Payment == null)
        {
            return NotFound();
        }

        return _mapper.Map<GuidewirePaymentStatusResult>(query);
    }

    /// <summary>
    /// This gets statues for a Payment sent to Guidewire for the internal VPay TransactionId
    /// </summary>
    /// <param name="transactionId">Internal VPay TransactionId</param>
    /// <param name="token"></param>
    /// <response code="404">Could not locate payment information</response>
    [HttpGet("transactions/{transactionId:long}/sent-statuses")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<ActionResult<GuidewirePaymentStatusResult>> GetStatusesForTransactionId([Required] long? transactionId, CancellationToken token)
    {
        var query = await _mediator.Send(new GetPaymentStatusTrackingForTransactionQuery(transactionId.GetValueOrDefault(0)), token).ConfigureAwait(false);

        if (query == null || query.Payment == null)
        {
            return NotFound();
        }

        return _mapper.Map<GuidewirePaymentStatusResult>(query);
    }
}
