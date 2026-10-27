using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.GatewayClients;

public class GetGatewayClientByIdQuery : IRequest<GatewayClient?>
{
    public long Id { get; }

    public GetGatewayClientByIdQuery(long id) => Id = id;
}

public class GetGatewayClientByIdQueryHandler : IRequestHandler<GetGatewayClientByIdQuery, GatewayClient?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public GetGatewayClientByIdQueryHandler(IDataConnection<SqlConnection> connection)
    {
        _connection = connection;
    }

    public Task<GatewayClient?> Handle(GetGatewayClientByIdQuery request, CancellationToken cancellationToken)
    {
        return _connection.GetFirstOrDefaultAsync<GatewayClient?, SqlConnection>("[dbo].[usp_GatewayClient_GetById]", new { request.Id }, commandType: CommandType.StoredProcedure);
    }
}
