using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public class PayeeStatusesQuery : IRequest<IReadOnlyCollection<PayeeStatus>>
{
    public PayeeStatusesQuery(string clientCode, string memberId)
    {
        ClientCode = clientCode;
        MemberId = memberId;
    }

    public string ClientCode { get; }

    public string MemberId { get; }
}

public class PayeeStatusesQueryHandler : IRequestHandler<PayeeStatusesQuery, IReadOnlyCollection<PayeeStatus>>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PayeeStatusesQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task<IReadOnlyCollection<PayeeStatus>> Handle(PayeeStatusesQuery request, CancellationToken cancellationToken)
    {
        var result = await _connection.GetAsync<PayeeStatus, SqlConnection>(
            "[dbo].[usp_PayeeStatus_GetByClientAndMemberId]",
            new
            {
                request.ClientCode,
                request.MemberId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.ToList().AsReadOnly();
    }
}
