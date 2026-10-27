using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Transactions;

public class GetPaymentByTransactionIdQuery : IRequest<TransactionPayment?>
{
    public long TransactionId { get; set; }

    public GetPaymentByTransactionIdQuery(long transactionId)
    {
        TransactionId = transactionId;
    }
}

public partial class GetPaymentByTransactionIdQueryHandler : IRequestHandler<GetPaymentByTransactionIdQuery, TransactionPayment?>
{
    internal static readonly string Sproc = "[dbo].[usp_TransactionPayment_GetByTransactionId]";

    private readonly IDataConnection<SqlConnection> _connection;
    private readonly ILogger<GetPaymentByTransactionIdQueryHandler> _logger;

    public GetPaymentByTransactionIdQueryHandler(
        IDataConnection<SqlConnection> connection,
        ILogger<GetPaymentByTransactionIdQueryHandler> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TransactionPayment?> Handle(GetPaymentByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        const string Action = "Find Transaction Payment";

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = Action,
            ["TransactionId"] = request.TransactionId,
        });

        try
        {
            return await _connection.GetFirstOrDefaultAsync<TransactionPayment?, SqlConnection>(
                Sproc,
                request,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.TransactionPaymentDbError(_logger, ex, request.TransactionId);
            throw;
        }
    }
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Transaction Payment DB: Transaction = {TransactionId}")]
        public static partial void TransactionPaymentDbError(ILogger logger, Exception ex, long transactionId);
    }
}
