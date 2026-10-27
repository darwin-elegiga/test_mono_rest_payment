using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Payments;

public record GuidewireUnloadProcessQuery(string ReasonCode) : IRequest<GuidewireUnloadProcess?>;

public class GuidewireUnloadProcessQueryHandler : IRequestHandler<GuidewireUnloadProcessQuery, GuidewireUnloadProcess?>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public GuidewireUnloadProcessQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task<GuidewireUnloadProcess?> Handle(GuidewireUnloadProcessQuery request, CancellationToken cancellationToken) =>
        await _connection.GetFirstOrDefaultAsync<GuidewireUnloadProcess?, SqlConnection>(
                "[dbo].[usp_GuidewireUnloadProcess_GetByReasonCode]",
                new
                {
                    request.ReasonCode
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
}
