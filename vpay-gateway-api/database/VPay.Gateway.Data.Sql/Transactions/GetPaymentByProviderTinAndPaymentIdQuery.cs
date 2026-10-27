using System.Data;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Data.Connection;

namespace VPay.Gateway.Data.Sql.Transactions;

public class GetPaymentByProviderTinAndPaymentIdQuery : IRequest<TransactionPayment?>
{
    public string ProviderTin { get; set; }
    public string PaymentId { get; set; }

    public GetPaymentByProviderTinAndPaymentIdQuery(string providerTin, string paymentId)
    {
        ProviderTin = providerTin;
        PaymentId = paymentId;
    }
}

public partial class GetPaymentByProviderTinAndPaymentIdQueryHandler : IRequestHandler<GetPaymentByProviderTinAndPaymentIdQuery, TransactionPayment?>
{
    internal static readonly string Sproc = "[dbo].[usp_TransactionPayment_GetByProviderTinAndPaymentId]";

    private readonly IDataConnection<SqlConnection> _connection;
    private readonly ILogger<GetPaymentByProviderTinAndPaymentIdQueryHandler> _logger;

    public GetPaymentByProviderTinAndPaymentIdQueryHandler(IDataConnection<SqlConnection> connection, ILogger<GetPaymentByProviderTinAndPaymentIdQueryHandler> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TransactionPayment?> Handle(GetPaymentByProviderTinAndPaymentIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        const string Action = "Find Transaction Payment";

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = Action,
            ["Provider TIN"] = request.ProviderTin,
            ["Payment ID"] = request.PaymentId,
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
            Log.TransactionPaymentDbError(_logger, ex, request.ProviderTin, request.PaymentId);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Error,
            Message = "ERROR Transaction Payment DB: Provider TIN = {ProviderTin}, Payment ID = {PaymentId}")]
        public static partial void TransactionPaymentDbError(ILogger logger, Exception ex, string providerTin, string paymentId);
    }
}
