using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.PepClaims;

public static class EnrollmentStatusUpdate
{
    public record Command(
        string CurrentProcessStatus,
        string NewProcessStatus,
        string? ClientCode = null,
        string? MemberId = null,
        string? ClaimId = null,
        int? MessageCount = 1,
        int? MinMessageAgeInMinutes = 30,
        int? MaxMessageAgeInDays = 60)
        : IRequest<IEnumerable<EnrollmentStatus>>;

    public class Handler : IRequestHandler<Command, IEnumerable<EnrollmentStatus>>
    {
        internal static readonly string Sproc = "[dbo].[usp_PayeeStatus_UpdateProcessStatus]";
        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection) =>
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        public Task<IEnumerable<EnrollmentStatus>> Handle(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = _connection.SaveAsync<EnrollmentStatus, SqlConnection>(
                Sproc,
                request,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            return result;
        }
    }
}
