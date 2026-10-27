using MediatR;
using Vpay.Gateway.Common.Vpay.Models;

namespace VPay.Gateway.Application;

public class GetVendorPaymentDetailQuery : IRequest<TransactionSearchResponseVendor>
{
    public long TransactionId { get; set; }

    public string PretransactionId { get; set; }

    public GetVendorPaymentDetailQuery(string pretransactionId,long transactionid)
    {
        TransactionId = transactionid;
        PretransactionId = pretransactionId;
    }
}
