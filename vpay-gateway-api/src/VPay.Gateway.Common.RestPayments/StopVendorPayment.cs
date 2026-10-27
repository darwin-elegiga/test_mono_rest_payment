using MediatR;
using Refit;

namespace VPay.Gateway.Common.RestPayments;

public static class StopVendorPayment
{
    private static readonly string _reasonCode = "4602";

    public record Query(string RequesterName, long TransactionId) : IRequest<IApiResponse<UnloadVendorPaymentResponse>>;

    public class Handler : IRequestHandler<Query, IApiResponse<UnloadVendorPaymentResponse>>
    {
        private readonly IRestPaymentApi _restPaymentApi;

        public Handler(IRestPaymentApi restPaymentApi) => _restPaymentApi =
            restPaymentApi ?? throw new ArgumentNullException(nameof(restPaymentApi));

        public async Task<IApiResponse<UnloadVendorPaymentResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _restPaymentApi.UnloadVendorPayment(request.RequesterName, request.TransactionId, _reasonCode, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
