using System.Data;
using MediatR;

namespace VPay.Gateway.Data.Sql.Payments;

public class PaymentVendorQuery : IRequest<VpayTransaction>
{
    public PaymentVendorQuery(string pretransactionid)
    {
        PreTransactionId = pretransactionid;
    }

    public string PreTransactionId { get; }
}
