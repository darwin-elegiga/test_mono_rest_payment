using MediatR;
using Refit;

namespace VPay.Gateway.Common.RestPayments;

public static class TransactionInfo
{
    public record Query(long TransactionId) : IRequest<IApiResponse<Response?>>;

    /// <summary>
    /// GetTransaction Response Object
    /// </summary>
    /// <param name="TransactionId">The Transaction Identifier</param>
    /// <param name="ClientCode">The Client Code for the Transaction</param>
    /// <param name="BillingEntityCode">The Billing Entity for the Transaction</param>
    public record Response(long TransactionId, string ClientCode, string BillingEntityCode);

    public class Handler : IRequestHandler<Query, IApiResponse<Response?>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<Response?>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _restPaymentApi.TransactionInfo(request.TransactionId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
