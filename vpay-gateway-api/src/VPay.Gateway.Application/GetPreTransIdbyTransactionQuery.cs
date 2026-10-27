using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace VPay.Gateway.Application;
public class GetPreTransIdbyTransactionQuery : IRequest<string>
{
    public long TransactionId { get; set; }
    public GetPreTransIdbyTransactionQuery(long transactionId)
    {
        TransactionId = transactionId;
    }
}
