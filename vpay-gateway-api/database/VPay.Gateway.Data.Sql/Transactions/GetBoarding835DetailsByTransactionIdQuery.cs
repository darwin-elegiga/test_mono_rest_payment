using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Transactions;

public class GetBoarding835DetailsByTransactionIdQuery : IRequest<TransactionBoarding835Details?>
{
    public long TransactionId { get; set; }
    public GetBoarding835DetailsByTransactionIdQuery(long transactionId)
    {
        TransactionId = transactionId;
    }
}

public partial class GetBoarding835DetailsByTransactionIdQueryHandler : IRequestHandler<GetBoarding835DetailsByTransactionIdQuery, TransactionBoarding835Details?>
{
    internal static readonly string Sproc = "[dbo].[usp_Transaction835Data_GetByTransactionId]";

    private readonly IDataConnection<SqlConnection> _connection;
    private readonly ILogger<GetNetworkByTransactionIdQueryHandler> _logger;

    public GetBoarding835DetailsByTransactionIdQueryHandler(
        IDataConnection<SqlConnection> connection,
        ILogger<GetNetworkByTransactionIdQueryHandler> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TransactionBoarding835Details?> Handle(GetBoarding835DetailsByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        const string Action = "Find Transaction Boarding 835 details";

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = Action,
            ["TransactionId"] = request.TransactionId,
        });

        try
        {
            return await _connection.GetFirstOrDefaultAsync<TransactionBoarding835Details?, SqlConnection>(
            Sproc,
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Transaction835DbError(_logger, ex, request.TransactionId);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Transaction 835 details DB: Transaction = {TransactionId}")]
        public static partial void Transaction835DbError(ILogger logger, Exception ex, long transactionId);
    }
}
