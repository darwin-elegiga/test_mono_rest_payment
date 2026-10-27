using MediatR;
using Refit;

namespace VPay.Gateway.Common.RestPayments;

public static class VoidPayment
{
    public record Query(string RequestedBy, long TransactionId) : IRequest<IApiResponse<Response>>;

    /// <summary>
    /// StopPayment Response Object
    /// </summary>
    /// <param name="ResponseCode">Response Code</param>
    /// <param name="ResponseDesc">Response Description</param>
    /// <param name="ReasonCode">Reason Code</param>
    /// <param name="ReasonDesc">Reason Description</param>
    /// <param name="SuccessCode">Success Code</param>
    /// <param name="SuccessDesc">Success Description</param>
    public record Response(string ResponseCode, string ResponseDesc, string ReasonCode, string ReasonDesc,
        string SuccessCode, string SuccessDesc);

    public class Handler : IRequestHandler<Query, IApiResponse<Response>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _restPaymentApi.VoidPayment(request.RequestedBy, request.TransactionId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
