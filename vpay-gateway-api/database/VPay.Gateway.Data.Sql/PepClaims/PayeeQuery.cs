using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public class PayeeQuery : IRequest<Payee>
{
    public PayeeQuery(string clientCode, string memberId)
    {
        ClientCode = clientCode;
        MemberId = memberId;
    }

    public string ClientCode { get; }

    public string MemberId { get; }
}

public class PayeeQueryHandler : IRequestHandler<PayeeQuery, Payee>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public PayeeQueryHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<Payee> Handle(PayeeQuery request, CancellationToken cancellationToken) =>
        _connection.GetFirstOrDefaultAsync<Payee, SqlConnection>(
            "[dbo].[usp_Payee_GetByClientAndMemberId]",
            new
            {
                request.ClientCode,
                request.MemberId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
