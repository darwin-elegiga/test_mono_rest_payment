using MediatR;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Common.Kubera;

public class GetTransactionsByRangeQuery : IRequest<IReadOnlyCollection<Transaction>>
{
    public GetTransactionsByRangeQuery(long startId, long endId)
    {
        StartId = startId;
        EndId = endId;
    }

    public long StartId { get; set; }

    public long EndId { get; set; }
}

public class GetTransactionsByRangeQueryHandler : IRequestHandler<GetTransactionsByRangeQuery, IReadOnlyCollection<Transaction>>
{
    private readonly IKuberaApi _api;

    public GetTransactionsByRangeQueryHandler(IKuberaApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<IReadOnlyCollection<Transaction>> Handle(GetTransactionsByRangeQuery request, CancellationToken cancellationToken)
    {
        // Have to do async/await because cannot implicitly determine type
        return await _api.GetTransactionsByRange(request.StartId, request.EndId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
