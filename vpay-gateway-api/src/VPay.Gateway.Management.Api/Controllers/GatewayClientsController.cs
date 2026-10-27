using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Gateway.Data.Sql.GatewayClients;
using VPay.Gateway.Management.Api.Models;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Management.Api.Controllers;

[Route("api/[controller]")]
[Produces("application/json")]
[ApiController]
public class GatewayClientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GatewayClientsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// This gets a gateway client's record by the internal id
    /// </summary>
    /// <param name="id">Internal id of the client</param>
    /// <param name="token"></param>
    /// <response code="404">Could not find the gateway client</response>
    [HttpGet("{id}", Name = RouteNames.Gateway.GetById)]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [Obsolete("Please use RGB to manage Gateway Clients")]
    public async Task<ActionResult<GatewayClientDto>> GetById(long id, CancellationToken token)
    {
        var client = await _mediator.Send(new GetGatewayClientByIdQuery(id), token).ConfigureAwait(false);
        if (client == null)
        {
            return NotFound();
        }

        return _mapper.Map<GatewayClientDto>(client);
    }

    /// <summary>
    /// Get's the Gateway client by their client Id
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="token"></param>
    /// <response code="404">Could not find the gateway client</response>
    [HttpGet("")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [Obsolete("Please use RGB to manage Gateway Clients")]
    public async Task<ActionResult<GatewayClientDto>> GetByClientId(string clientId, CancellationToken token)
    {
        var client = await _mediator.Send(new GetGatewayClientByClientIdQuery(clientId), token).ConfigureAwait(false);
        if (client == null)
        {
            return NotFound();
        }

        return _mapper.Map<GatewayClientDto>(client);
    }

    /// <summary>
    /// Creates a new Gateway Client. Please keep the Client Secret you setup because it will not be available through this application again.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="token"></param>
    /// <response code="400">Entity has been formatted wrong</response>
    /// <response code="409">The Client Id already exists and cannot be created again</response>
    [HttpPost]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status409Conflict)]
    [Obsolete("Please use RGB to manage Gateway Clients")]
    public async Task<ActionResult<GatewayClientCreateResult>> PostCreateClient(GatewayClientCreateParam entity, CancellationToken token)
    {
        return StatusCode(Status410Gone);
    }

    /// <summary>
    /// Expire's the Gateway Client record
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="token"></param>
    /// <response code="400">Entity has been formatted wrong</response>
    /// <response code="404">Could not find the gateway client</response>
    [HttpPost("expire")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [Obsolete("Please use RGB to manage Gateway Clients")]
    public async Task<ActionResult<GatewayClientDto>> PostExpire(GatewayClientExpireParam entity, CancellationToken token)
    {
        return StatusCode(Status410Gone);
    }
}
