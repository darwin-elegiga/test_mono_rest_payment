using MediatR;

namespace VPay.Gateway.Application;

public class GetTransactionbyPreTransIdQuery :IRequest<long>
{
    public string PreTransactionId { get; set; }

    public GetTransactionbyPreTransIdQuery(string preTransactionid)
    {
        PreTransactionId = preTransactionid;
    }
}
