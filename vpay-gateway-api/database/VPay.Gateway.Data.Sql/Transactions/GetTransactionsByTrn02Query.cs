using MediatR;
using Microsoft.Data.SqlClient;
using VPay.Gateway.Data.Connection;
using VPay.Gateway.Data.Paging;

namespace VPay.Gateway.Data.Sql.Transactions;
public static class GetTransactionsByTrn02
{
    //private Trn02SearchRequest trn02SearchRequest;
    public record Query(
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        string? Trn02 = null,
        string? ProviderTin = null,
        int? Page = Defaults.DefaultPage,
        int? PageSize = Defaults.DefaultPageSize)
        : IRequest<PagedResult<TransactionsTrn02Details>>;

    public class Handler : IRequestHandler<Query, PagedResult<TransactionsTrn02Details>>
    {
        internal static readonly string Sproc = "[dbo].[usp_GetTransactionsByTrn02]";
        private readonly IDataConnection<SqlConnection> _connection;

        public Handler(IDataConnection<SqlConnection> connection) =>
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        public async Task<PagedResult<TransactionsTrn02Details>> Handle(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _connection
                .GetPagedResultAsync<TransactionsTrn02Details>(Sproc, request, cancellationToken)
                .ConfigureAwait(false);
            return result;
        }


    }
}
