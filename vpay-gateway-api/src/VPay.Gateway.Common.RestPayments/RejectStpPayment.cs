// Ignore Spelling: Stp

using MediatR;
using Refit;

namespace VPay.Gateway.Common.RestPayments;

public static class RejectStpPayment
{
    public record Request(
        string NetworkName,
        long TransactionId,
        string RejectionCode,
        string RejectionReason)
        : IRequest<IApiResponse<Response>>;

    public record Response(
        string ResponseCode,
        string ResponseDesc,
        string ReasonCode,
        string ReasonDesc,
        string SuccessCode,
        string SuccessDesc);

    public class Handler : IRequestHandler<Request, IApiResponse<Response>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            return await _restPaymentApi
                .RejectStpPayment(request.NetworkName, request.TransactionId, request.RejectionCode, request.RejectionReason, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
