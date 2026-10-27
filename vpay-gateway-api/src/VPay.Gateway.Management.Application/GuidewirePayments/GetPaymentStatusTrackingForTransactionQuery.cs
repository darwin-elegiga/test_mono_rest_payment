using MediatR;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

public class GetPaymentStatusTrackingForTransactionQuery : IRequest<GetPaymentStatusTrackingResult>
{
    public GetPaymentStatusTrackingForTransactionQuery(long transactionId)
    {
        TransactionId = transactionId;
    }

    public long TransactionId { get; }
}

public class GetPaymentStatusTrackingForTransactionQueryHandler : IRequestHandler<GetPaymentStatusTrackingForTransactionQuery, GetPaymentStatusTrackingResult>
{
    private readonly IMediator _mediator;

    public GetPaymentStatusTrackingForTransactionQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<GetPaymentStatusTrackingResult> Handle(GetPaymentStatusTrackingForTransactionQuery request, CancellationToken cancellationToken)
    {
        var result = new GetPaymentStatusTrackingResult();

        result.Payment = await _mediator.Send(new PaymentForTransactionQuery(request.TransactionId), cancellationToken).ConfigureAwait(false);

        if (result.Payment != null)
        {
            result.StatusTrackings = (await _mediator.Send(new PaymentStatusTrackingQuery(result.Payment.Id), cancellationToken).ConfigureAwait(false)).ToList();
        }

        return result;
    }
}
