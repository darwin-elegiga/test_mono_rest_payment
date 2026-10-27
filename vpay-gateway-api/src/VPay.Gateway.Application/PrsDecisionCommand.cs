using MediatR;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application;

public class PrsDecisionCommand : IRequest<PrsDecisionResponse>
{
    public PrsDecisionCommand(
        string billingEntityCode,
        string taxId,
        string zipCode,
        decimal? amount,
        DateTime? date,
        string clientCode)
    {
        BillingEntityCode = billingEntityCode;
        TaxId = taxId;
        ZipCode = zipCode;
        Amount = amount;
        Date = date;
        ClientCode = clientCode;
    }

    public string BillingEntityCode { get; }

    public string TaxId { get; }

    public string ZipCode { get; }

    public decimal? Amount { get; }

    public DateTime? Date { get; }

    public string ClientCode { get; }
}

public class PrsDecisionCommandHandler : IRequestHandler<PrsDecisionCommand, PrsDecisionResponse>
{
    private readonly IPreferencingApi _preferencingApi;

    public PrsDecisionCommandHandler(IPreferencingApi preferencingApi)
    {
        _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
    }

    public async Task<PrsDecisionResponse> Handle(PrsDecisionCommand request, CancellationToken cancellationToken)
    {
        var prsDecisionRequest = new PrsDecisionRequest
        {
            OriginCode = string.Empty,
            ClientCode = request.ClientCode,
            BillingEntityCode = request.BillingEntityCode,
            TaxId = request.TaxId,
            ZipCode = request.ZipCode,
            Amount = request.Amount,
            Date = request.Date
        };

        return await _preferencingApi.PrsDecision(prsDecisionRequest, cancellationToken).ConfigureAwait(false);
    }
}
