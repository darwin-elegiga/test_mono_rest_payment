using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Paging;

namespace VPay.Gateway.Data.Sql.PepClaims;

public static class EnrollmentStatusSearch
{
    public record Query(
        string? CurrentProcessStatus = null,
        string? ClientCode = null,
        string? MemberId = null,
        string? ClaimId = null,
        int? Page = Defaults.DefaultPage,
        int? PageSize = Defaults.DefaultPageSize)
        : IRequest<PagedResult<EnrollmentStatus>>;

    public class Handler : IRequestHandler<Query, PagedResult<EnrollmentStatus>>
    {
        internal static readonly string Sproc = "[dbo].[usp_PayeeStatus_GetByProcessStatus]";
        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection) =>
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        public async Task<PagedResult<EnrollmentStatus>> Handle(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _connection
                .GetPagedResultAsync<EnrollmentStatus>(Sproc, request, cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
    }
}
