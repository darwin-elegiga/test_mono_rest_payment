using System.Net;
using FluentValidation;
using MediatR;
using Refit;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application;

public static class GetOptumPrsConfiguration
{
    public record Query(string PayerId, string SubPayerId) : IRequest<OptumPrsConfigurationDto?>;

    public sealed class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(x => x.PayerId)
                .NotEmpty()
                .MaximumLength(10);

            RuleFor(x => x.SubPayerId)
                .NotEmpty()
                .MaximumLength(10);
        }
    }

    public class Handler : IRequestHandler<Query, OptumPrsConfigurationDto?>
    {
        protected readonly IMediator _mediator;
        private readonly QueryValidator _queryValidator = new();

        public Handler(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<OptumPrsConfigurationDto?> Handle(Query request, CancellationToken cancellationToken)
        {
            var payerId = request.PayerId;
            var subPayerId = request.SubPayerId;

            await _queryValidator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

            OptumSubPayerDto? optumSubPayer;

            try
            {
                optumSubPayer = await _mediator.Send(new OptumSubPayerQuery(subPayerId), cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ValidationException($"Optum PRS Mapping not found for sub-payer {subPayerId}");
            }
            catch (Exception)
            {
                throw new ValidationException($"Optum PRS Mapping is invalid for sub-payer {subPayerId}");
            }

            if (optumSubPayer == null)
            {
                throw new ValidationException($"Optum PRS Mapping not found for sub-payer {subPayerId}");
            }
            else if (optumSubPayer.OptumPayorId != payerId)
            {
                throw new ValidationException($"Optum PRS Mapping not found for payer {payerId}");
            }

            var clientCode = optumSubPayer.ClientCode;
            var beCode = optumSubPayer.BillingEntityCode;

            OptumPrsConfigurationDto? prsConfiguration;

            try
            {
                var originClient = await _mediator.Send(new OriginClientQuery(clientCode), cancellationToken).ConfigureAwait(false);

                var originCode = originClient?.Origin?.OriginCode;

                prsConfiguration = await _mediator.Send(new OptumPrsConfigurationQuery(originCode, clientCode, beCode), cancellationToken).ConfigureAwait(false);
            }
            catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ValidationException($"Optum PRS Configuration not found for payer {payerId} and sub-payer {subPayerId}");
            }
            catch (Exception)
            {
                throw new ValidationException($"Optum PRS Configuration is invalid for payer {payerId} and sub-payer {subPayerId}");
            }

            if (prsConfiguration == null)
            {
                throw new ValidationException($"Optum PRS Configuration not found for payer {payerId} and sub-payer {subPayerId}");
            }

            return prsConfiguration;
        }
    }
}
