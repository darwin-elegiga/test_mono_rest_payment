using MediatR;

namespace VPay.Gateway.Data.Sql.Payments;
public class PaymentTransactionQuery : IRequest<VendorTransaction>
{
    public PaymentTransactionQuery(long transactionid)
    {
        TransactionId = transactionid;
    }

    public long TransactionId { get; }
}
