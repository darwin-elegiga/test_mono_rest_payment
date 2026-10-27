using Refit;
using VPay.Gateway.Common.MemberClaims.Models;

namespace VPay.Gateway.Common.MemberClaims;

public interface IMemberClaimsApi
{
    [Post("/api/payment/receive-payment-info")]
    Task<IApiResponse<dynamic>> ReceivePaymentInfo(VendorPaymentDecision paymentInfo, CancellationToken cancellationToken = default);
}
