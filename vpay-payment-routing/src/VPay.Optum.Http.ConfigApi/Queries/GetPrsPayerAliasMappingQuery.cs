using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Http.ConfigApi.Queries;

public static class GetPrsPayerAliasMapping
{
    public record Query(string PayerAlias) : IRequest<PrsPayerAliasMappingDto>;

    public class PrsPayerAliasMappingQueryHandler : IRequestHandler<Query, PrsPayerAliasMappingDto>
    {
        private readonly IConfigApi _configApi;

        public PrsPayerAliasMappingQueryHandler(IConfigApi configApi)
        {
            _configApi = configApi ?? throw new ArgumentNullException(nameof(configApi));
        }

        public Task<PrsPayerAliasMappingDto> Handle(Query request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.PayerAlias?.Trim()))
            {
                throw new ArgumentException($"PayerAlias cannot be empty");
            }
            return HandleAsync(request, cancellationToken);
        }

        public async Task<PrsPayerAliasMappingDto> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var result = await _configApi.GetPrsPayerAliasMapping(request.PayerAlias, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccessStatusCode && result.Content is not null)
            {
                return result.Content;
            }

            throw new ArgumentNullException($"VPRS Mapping not found for Payer Alias: '{request.PayerAlias}', StatusCode: {result.StatusCode}");
        }
    }
}

