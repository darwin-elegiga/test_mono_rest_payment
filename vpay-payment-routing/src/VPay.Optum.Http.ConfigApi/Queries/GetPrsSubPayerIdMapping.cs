using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VPay.Optum.Http.ConfigApi.Models;

namespace VPay.Optum.Http.ConfigApi.Queries;

public static class GetPrsSubPayerIdMapping
{
    public record Query(string SubPayerId) : IRequest<PrsSubPayerMappingDto>;

    public class Handler : IRequestHandler<Query, PrsSubPayerMappingDto>
    {
        private readonly IConfigApi _configApi;

        public Handler(IConfigApi configApi)
        {
            _configApi = configApi ?? throw new ArgumentNullException(nameof(configApi));
        }

        public Task<PrsSubPayerMappingDto> Handle(Query request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.SubPayerId?.Trim()))
            {
                throw new ArgumentException($"SubPayerId cannot be empty");
            }
            return HandleAsync(request, cancellationToken);
        }

        public async Task<PrsSubPayerMappingDto> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var result = await _configApi.GetPrsSubPayerMapping(request.SubPayerId, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccessStatusCode && result.Content is not null)
            {
                return result.Content;
            }

            throw new ArgumentNullException($"VPRS Mapping not found for SubPayerId: '{request.SubPayerId}', StatusCode: {result.StatusCode}");
        }
    }
}

