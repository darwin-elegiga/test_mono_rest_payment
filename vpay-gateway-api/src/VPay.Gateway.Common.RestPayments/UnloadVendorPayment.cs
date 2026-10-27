using MediatR;
using Refit;

namespace VPay.Gateway.Common.RestPayments;

public static class UnloadVendorPayment
{
    private static string _defaultReasonCode = "4701";

    public record Query(string RequesterName, long TransactionId, string? ReasonCode = null) : IRequest<IApiResponse<UnloadVendorPaymentResponse>>;

    public class Handler : IRequestHandler<Query, IApiResponse<UnloadVendorPaymentResponse>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<UnloadVendorPaymentResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var reasonCode = string.IsNullOrWhiteSpace(request.ReasonCode) ? _defaultReasonCode : request.ReasonCode;
            return await _restPaymentApi.UnloadVendorPayment(request.RequesterName, request.TransactionId, reasonCode, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
