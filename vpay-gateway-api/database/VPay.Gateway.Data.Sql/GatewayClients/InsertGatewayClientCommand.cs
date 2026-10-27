using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.GatewayClients;

public class InsertGatewayClientCommand : IRequest<GatewayClient>
{
    public InsertGatewayClientCommand(
        string clientId,
        string clientSecret,
        string originCode,
        string clientCode,
        string user)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        OriginCode = originCode;
        ClientCode = clientCode;
        User = user;
    }

    public string ClientId { get; }

    public string ClientSecret { get; }

    public string OriginCode { get; }

    public string ClientCode { get; }

    public string User { get; }
}

public class InsertGatewayClientCommandHandler : IRequestHandler<InsertGatewayClientCommand, GatewayClient>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public InsertGatewayClientCommandHandler(IDataConnection<SqlConnection> connection)
    {
        _connection = connection;
    }

    public Task<GatewayClient> Handle(InsertGatewayClientCommand request, CancellationToken cancellationToken)
    {
        return _connection.SaveFirstOrDefaultAsync<GatewayClient, SqlConnection>("[dbo].[usp_GatewayClient_Insert]",
            new
            {
                request.ClientId,
                request.ClientSecret,
                request.OriginCode,
                request.ClientCode,
                request.User
            },
            commandType: CommandType.StoredProcedure);
    }
}
