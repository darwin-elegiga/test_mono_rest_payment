using MediatR;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.Application;

public class GetTransactionbyPreTransIdQueryHandler : IRequestHandler<GetTransactionbyPreTransIdQuery, long>
{
    private readonly IMediator _mediator;

    public GetTransactionbyPreTransIdQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<long> Handle(GetTransactionbyPreTransIdQuery request, CancellationToken cancellationToken)
    {
        long vpayTransactionID = 0;

        if (vpayTransactionID == 0)
        {
            var transid = await _mediator.Send(new PaymentVendorQuery(request.PreTransactionId), cancellationToken).ConfigureAwait(false);

            if (transid == null)
            {
                throw new InvalidOperationException("Transaction Id could not be resolved.");
            }
            vpayTransactionID = transid.VpayTransactionID;
        }
        return vpayTransactionID;
    }
}
