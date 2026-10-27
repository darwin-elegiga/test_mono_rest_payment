using MediatR;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.Management.Application.GuidewirePayments;

public class GetPaymentStatusTrackingQuery : IRequest<GetPaymentStatusTrackingResult>
{
    public GetPaymentStatusTrackingQuery(string clientCode, string clientPaymentId)
    {
        ClientCode = clientCode;
        ClientPaymentId = clientPaymentId;
    }

    public string ClientCode { get; }

    public string ClientPaymentId { get; }
}

public class GetPaymentStatusTrackingQueryHandler : IRequestHandler<GetPaymentStatusTrackingQuery, GetPaymentStatusTrackingResult>
{
    private readonly IMediator _mediator;

    public GetPaymentStatusTrackingQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<GetPaymentStatusTrackingResult> Handle(GetPaymentStatusTrackingQuery request, CancellationToken cancellationToken)
    {
        var result = new GetPaymentStatusTrackingResult();

        result.Payment = await _mediator.Send(new PaymentQuery(request.ClientCode, request.ClientPaymentId), cancellationToken).ConfigureAwait(false);

        if (result.Payment != null)
        {
            result.StatusTrackings = (await _mediator.Send(new PaymentStatusTrackingQuery(result.Payment.Id), cancellationToken).ConfigureAwait(false)).ToList();
        }

        return result;
    }
}
