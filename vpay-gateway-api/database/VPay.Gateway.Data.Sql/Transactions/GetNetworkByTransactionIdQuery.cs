using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Transactions;

public class GetNetworkByTransactionIdQuery : IRequest<TransactionNetwork?>
{
    public long TransactionId { get; set; }

    public GetNetworkByTransactionIdQuery(long transactionId)
    {
        TransactionId = transactionId;
    }
}

public partial class GetNetworkByTransactionIdQueryHandler : IRequestHandler<GetNetworkByTransactionIdQuery, TransactionNetwork?>
{
    internal static readonly string Sproc = "[dbo].[usp_TransactionNetwork_GetByTransactionId]";

    private readonly IDataConnection<SqlConnection> _connection;
    private readonly ILogger<GetNetworkByTransactionIdQueryHandler> _logger;

    public GetNetworkByTransactionIdQueryHandler(
        IDataConnection<SqlConnection> connection,
        ILogger<GetNetworkByTransactionIdQueryHandler> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TransactionNetwork?> Handle(GetNetworkByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        const string Action = "Find Transaction Network";

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = Action,
            ["TransactionId"] = request.TransactionId,
        });

        try
        {
            return await _connection.GetFirstOrDefaultAsync<TransactionNetwork?, SqlConnection>(
                Sproc,
                request,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.TransactionNetworkDbError(_logger, ex, request.TransactionId);
            throw;
        }
    }
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Transaction Network DB: Transaction = {TransactionId}")]
        public static partial void TransactionNetworkDbError(ILogger logger, Exception ex, long transactionId);
    }
}
