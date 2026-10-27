using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public class InsertPayeeCommand : IRequest<Payee>
{
    public InsertPayeeCommand(string originCode, string clientCode, string memberId, string partyName, string partyEmail, string phone)
    {
        OriginCode = originCode;
        ClientCode = clientCode;
        MemberId = memberId;
        PartyName = partyName;
        PartyEmail = partyEmail;
        Phone = phone;
    }

    public string OriginCode { get; }

    public string ClientCode { get; }

    public string MemberId { get; }

    public string PartyName { get; }

    public string PartyEmail { get; }

    public string Phone { get; }
}

public class InsertPayeeCommandHandler : IRequestHandler<InsertPayeeCommand, Payee>
{
    private readonly IDataConnection<SqlConnection> _connection;

    public InsertPayeeCommandHandler(IDataConnection<SqlConnection> connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public Task<Payee> Handle(InsertPayeeCommand request, CancellationToken cancellationToken) =>
        _connection.SaveFirstOrDefaultAsync<Payee, SqlConnection>(
            "[dbo].[usp_Payee_Insert]",
            new
            {
                request.OriginCode,
                request.ClientCode,
                request.MemberId,
                Name = request.PartyName,
                Email = request.PartyEmail,
                request.Phone
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
}
