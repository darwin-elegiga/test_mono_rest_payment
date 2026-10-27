using MediatR;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Common.Kubera;

public class GetTransactionsByIdListQuery : IRequest<IReadOnlyCollection<Transaction>>
{
    public GetTransactionsByIdListQuery(List<long> ids) => Ids = ids;

    public IReadOnlyCollection<long> Ids { get; }
}

public class GetTransactionsByIdListQueryHandler : IRequestHandler<GetTransactionsByIdListQuery, IReadOnlyCollection<Transaction>>
{
    private readonly IKuberaApi _api;

    public GetTransactionsByIdListQueryHandler(IKuberaApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<IReadOnlyCollection<Transaction>> Handle(GetTransactionsByIdListQuery request, CancellationToken cancellationToken)
    {
        // Have to do async/await because cannot implicitly determine type
        return await _api.GetTransactionsByIds(request.Ids.ToArray(), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
