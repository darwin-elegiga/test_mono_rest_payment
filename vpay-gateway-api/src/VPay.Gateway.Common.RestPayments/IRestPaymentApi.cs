using Refit;
using static VPay.Gateway.Common.RestPayments.AddBankAccount;
using static VPay.Gateway.Common.RestPayments.ExpireBankAccount;

namespace VPay.Gateway.Common.RestPayments;

/// <summary>
/// Interface defining the calls in the REST Payment API that are used
/// </summary>
public interface IRestPaymentApi
{
    [Delete("/api/void-payment")]
    Task<IApiResponse<VoidPayment.Response>> VoidPayment(string requester, long transactionId, CancellationToken cancellationToken);

    [Get("/api/void-payment/transaction-info")]
    Task<IApiResponse<TransactionInfo.Response?>> TransactionInfo(long transactionId, CancellationToken cancellationToken);

    [Delete("/api/stppayment/reject-stp-payment")]
    Task<IApiResponse<RejectStpPayment.Response>> RejectStpPayment(string networkName, long transactionId, string rejectionCode, string rejectionReason, CancellationToken cancellationToken);

    [Delete("/api/vendor-payment/unload-payment")]
    Task<IApiResponse<UnloadVendorPaymentResponse>> UnloadVendorPayment(string requester, long transactionId, string reasonCode, CancellationToken cancellationToken);

    [Post("/api/vendor-payment/add-bank-account")]
    Task<IApiResponse<AddBankAccount.AddBankAccountResponse>> AddBankAccount(Command command, CancellationToken cancellationToken);

    [Post("/api/vendor-payment/expire-bank-account")]
    Task<IApiResponse<ExpireBankAccount.ExpireBankAccountResponse>> ExpireBankAccount(ExpireBankAccountCommand command, CancellationToken cancellationToken);
}
