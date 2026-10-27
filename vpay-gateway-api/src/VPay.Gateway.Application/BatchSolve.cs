using MediatR;
using VPay.Gateway.Common.Preferencing;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application.BatchSolve;

public record Query(IEnumerable<BatchSolveRequest> Batch) : IRequest<IEnumerable<BatchSolveResponse>>;

public class Handler : IRequestHandler<Query, IEnumerable<BatchSolveResponse>>
{
    private readonly IPreferencingApi _preferencingApi;

    public Handler(IPreferencingApi preferencingApi)
    {
        _preferencingApi = preferencingApi ?? throw new ArgumentNullException(nameof(preferencingApi));
    }

    public async Task<IEnumerable<BatchSolveResponse>> Handle(Query request, CancellationToken cancellationToken)
    {
        return await _preferencingApi.BatchSolve(request.Batch, cancellationToken).ConfigureAwait(false);
    }
}
