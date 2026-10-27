using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.GatewayClients;

public class UpdateGatewayClientCommand : IRequest<GatewayClient?>
{
    public UpdateGatewayClientCommand(
        long id,
        string clientId,
        string clientSecret,
        string originCode,
        string clientCode,
        DateTime? expiredDateTime,
        string user)
    {
        Id = id;
        ClientId = clientId;
        ClientSecret = clientSecret;
        OriginCode = originCode;
        ClientCode = clientCode;
        ExpiredDateTime = expiredDateTime;
        User = user;
    }

    public long Id { get; }

    public string ClientId { get; }

    public string ClientSecret { get; }

    public string OriginCode { get; }

    public string ClientCode { get; }

    public DateTime? ExpiredDateTime { get; }

    public string User { get; }
}

public class UpdateGatewayClientCommandHandler : IRequestHandler<UpdateGatewayClientCommand, GatewayClient?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public UpdateGatewayClientCommandHandler(IDataConnection<SqlConnection> connection)
    {
        _connection = connection;
    }

    public Task<GatewayClient?> Handle(UpdateGatewayClientCommand request, CancellationToken cancellationToken)
    {
        return _connection.SaveFirstOrDefaultAsync<GatewayClient?, SqlConnection>("[dbo].[usp_GatewayClient_Update]",
            new
            {
                request.Id,
                request.ClientId,
                request.ClientSecret,
                request.OriginCode,
                request.ClientCode,
                request.ExpiredDateTime,
                request.User
            },
            commandType: CommandType.StoredProcedure);
    }
}
