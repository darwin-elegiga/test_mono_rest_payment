using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.GatewayClients;

public class GetGatewayClientByClientIdQuery : IRequest<GatewayClient?>
{
    public string ClientId { get; }

    public GetGatewayClientByClientIdQuery(string clientId) => ClientId = clientId;
}

public class GetGatewayClientByClientIdQueryHandler : IRequestHandler<GetGatewayClientByClientIdQuery, GatewayClient?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public GetGatewayClientByClientIdQueryHandler(IDataConnection<SqlConnection> connection)
    {
        _connection = connection;
    }

    public Task<GatewayClient?> Handle(GetGatewayClientByClientIdQuery request, CancellationToken cancellationToken)
    {
        return _connection.GetFirstOrDefaultAsync<GatewayClient?, SqlConnection>("[dbo].[usp_GatewayClient_GetByClientId]", new { request.ClientId }, commandType: CommandType.StoredProcedure);
    }
}
