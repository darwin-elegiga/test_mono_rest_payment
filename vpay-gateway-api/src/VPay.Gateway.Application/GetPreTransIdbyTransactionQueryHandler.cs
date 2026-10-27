using System;
using System.Collections.Generic;
using MediatR;
using VPay.Gateway.Data.Sql.Payments;

namespace VPay.Gateway.Application;
public class GetPreTransIdbyTransactionQueryHandler : IRequestHandler<GetPreTransIdbyTransactionQuery, string>
{
    private readonly IMediator _mediator;

    public GetPreTransIdbyTransactionQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<string> Handle(GetPreTransIdbyTransactionQuery request, CancellationToken cancellationToken)
    {
        string preTransactionID = "";

        if (string.IsNullOrEmpty(preTransactionID))
        {
            var pretransid = await _mediator.Send(new PaymentTransactionQuery(request.TransactionId), cancellationToken).ConfigureAwait(false);
            preTransactionID = pretransid != null ? pretransid.PreTransactionID : "";
        }
        return preTransactionID;
    }
}
