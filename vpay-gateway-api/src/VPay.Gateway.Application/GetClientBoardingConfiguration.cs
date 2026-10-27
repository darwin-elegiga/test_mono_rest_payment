using System.Net;
using FluentValidation;
using MediatR;
using Refit;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application;

public static class GetClientBoardingConfiguration
{
    public record Query(string ConfigurationName, string OriginCode, string ClientCode) : IRequest<ClientConfigurationDto>;

    public class Handler : IRequestHandler<Query, ClientConfigurationDto>
    {
        private readonly IMediator _mediator;

        public Handler(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<ClientConfigurationDto> Handle(Query request, CancellationToken cancellationToken)
        {
            ClientConfigurationDto? clientConfig;

            try
            {
                clientConfig = await _mediator
                    .Send(new ClientBoardingConfigurationQuery(request.ConfigurationName, request.OriginCode, request.ClientCode), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ValidationException($"Client Boarding Configuration not found for client {request.ClientCode}");
            }
            catch (Exception)
            {
                throw new ValidationException($"Client Boarding Configuration is invalid for client {request.ClientCode}");
            }

            if (clientConfig == null)
            {
                throw new ValidationException($"Client Boarding Configuration not found for client {request.ClientCode}");
            }

            return clientConfig;
        }
    }
}
