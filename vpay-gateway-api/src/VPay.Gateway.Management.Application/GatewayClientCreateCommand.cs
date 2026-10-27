using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Common.Identity;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Data.Sql.Exceptions;
using VPay.Gateway.Data.Sql.GatewayClients;

namespace VPay.Gateway.Management.Application;

public class GatewayClientCreateCommand : IRequest<GatewayClient>
{
    public GatewayClientCreateCommand(string clientId, string clientSecret, string clientCode, string user)
    {
        ClientId = clientId.Trim();
        ClientSecret = clientSecret.Trim();
        ClientCode = clientCode.Trim();
        User = user;
    }

    public string ClientId { get; }

    public string ClientSecret { get; }

    public string ClientCode { get; }

    public string User { get; }
}

public class GatewayClientCreateCommandHandler : IRequestHandler<GatewayClientCreateCommand, GatewayClient>
{
    private readonly IMediator _mediator;
    private readonly IPasswordHasher _hasher;

    public GatewayClientCreateCommandHandler(IMediator mediator, IPasswordHasher hasher)
    {
        _mediator = mediator;
        _hasher = hasher;
    }

    public async Task<GatewayClient> Handle(GatewayClientCreateCommand request, CancellationToken cancellationToken)
    {
        var findOrigin = await _mediator.Send(new OriginClientQuery(request.ClientCode), cancellationToken).ConfigureAwait(false);

        if (findOrigin == null || findOrigin.Origin == null)
        {
            throw new ArgumentException("Could not find ClientCode", nameof(request.ClientCode));
        }

        var findClient = await _mediator.Send(new GetGatewayClientByClientIdQuery(request.ClientId), cancellationToken).ConfigureAwait(false);

        if (findClient != null)
        {
            throw new DuplicateEntityException("Client Id already exists");
        }

        try
        {
            string hashedSecret = _hasher.Hash(request.ClientSecret);

            //TODO: add ability to look up the OriginCode and validate the ClientCode
            var query = new InsertGatewayClientCommand(request.ClientId, hashedSecret, findOrigin.Origin.OriginCode, request.ClientCode, request.User);

            return await _mediator.Send(query, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.IsUniqueConstraintOrIndexException())
        {
            throw new DuplicateEntityException("Client Id already exists", ex);
        }
    }
}
