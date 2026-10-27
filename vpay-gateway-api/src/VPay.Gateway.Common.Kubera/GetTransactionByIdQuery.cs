using MediatR;
using VPay.Gateway.Common.Kubera.Models;

namespace VPay.Gateway.Common.Kubera;

public class GetTransactionByIdQuery : IRequest<Transaction?>
{
    public GetTransactionByIdQuery(long id) => Id = id;

    public long Id { get; set; }
}

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Transaction?>
{
    private readonly IKuberaApi _api;

    public GetTransactionByIdQueryHandler(IKuberaApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<Transaction?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _api.GetTransactionsByIds(new[] { request.Id }, cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.FirstOrDefault();
    }
}
