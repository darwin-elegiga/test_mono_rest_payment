using MediatR;
using VPay.Gateway.Data.Sql.GatewayClients;

namespace VPay.Gateway.Management.Application;

public class GatewayClientExpireCommand : IRequest<GatewayClient?>
{
    public GatewayClientExpireCommand(string clientId, string user)
    {
        ClientId = clientId.Trim();
        User = user;
    }

    public string ClientId { get; }

    public string User { get; }
}

public class GatewayClientExpireCommandHandler : IRequestHandler<GatewayClientExpireCommand, GatewayClient?>
{
    private readonly IMediator _mediator;

    public GatewayClientExpireCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<GatewayClient?> Handle(GatewayClientExpireCommand request, CancellationToken cancellationToken)
    {
        var entity = await _mediator.Send(new GetGatewayClientByClientIdQuery(request.ClientId), cancellationToken).ConfigureAwait(false);

        if (entity == null)
        {
            return null;
        }

        if (entity.ExpiredDateTime.HasValue)
        {
            return entity;
        }

        return await _mediator.Send(new UpdateGatewayClientCommand(entity.Id, entity.ClientId, entity.ClientSecret, entity.OriginCode, entity.ClientCode, DateTime.Now, request.User)).ConfigureAwait(false);
    }
}
