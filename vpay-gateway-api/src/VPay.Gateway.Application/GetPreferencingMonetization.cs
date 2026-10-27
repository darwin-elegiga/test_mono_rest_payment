using MediatR;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application;

public static class GetPreferencingMonetization
{
    public record Query(MonetizationRequest Request) : IRequest<MonetizationResult>;

    public class Handler : IRequestHandler<Query, MonetizationResult>
    {
        private readonly IPreferencingApi _preferencingApi;

        public Handler(IPreferencingApi preferencingApi)
        {
            _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
        }

        public async Task<MonetizationResult> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _preferencingApi.GetMonetizedStatus(request.Request, cancellationToken).ConfigureAwait(false);
                var monResult = new MonetizationResult
                {
                    CorrelationId = result.CorrelationID,
                    ValidatedPayee = result.Monetized
                };

                return monResult;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while processing the monetization request.", ex);
            }
        }
    }
}
