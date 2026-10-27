using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing.Models;

namespace VPay.Gateway.Application.BatchSolveRequestBuilder;

/// <summary>
/// Call <see cref="PreferenceLookupDto"/> for batch of <see cref="PreferenceLookupDto"/> and <see cref="OptumSubPayerDto"/>
/// </summary>
/// <param name="Batch"></param>
/// <exception cref="NoPrsConfigurationValidationException" />
public record Query(IEnumerable<PreferenceLookupDto> Batch, IEnumerable<OptumSubPayerDto> Mappings) : IRequest<List<BatchSolveRequest>>;

public class Handler : IRequestHandler<Query, List<BatchSolveRequest>>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    public Handler(ILogger<Handler> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<List<BatchSolveRequest>> Handle(Query request, CancellationToken cancellationToken)
    {
        var batch = new List<BatchSolveRequest>();

        var result = new ValidationResult();

        foreach (var groupByClient in from l in request.Batch.Select((Item, Index) => new { Item, Index })
                                      join m in request.Mappings on l.Item.SubPayerID equals m.OptumSubPayorId
                                      group new { l.Index, Lookup = l.Item, Mapping = m, } by m.ClientCode)
        {
            using var _ = _logger.ClientScope(groupByClient.Key);

            var entities = await _mediator.Send(new BillingEntitiesQuery(groupByClient.Key), cancellationToken).ConfigureAwait(false);

            if (entities.Count < 1)
            {
                _logger.BillingEntitiesNotFoundForClient(groupByClient.Key);
                result.Errors.AddRange(groupByClient.SelectMany(g => NoTypeForClient(g.Index)));
                continue;
            }

            foreach (var join in groupByClient.ExceptBy(entities.Select(e => e.BillingEntityCode), i => i.Mapping.BillingEntityCode))
            {
                _logger.BillingEntitiesNotFoundForClientEntity(join.Mapping.ClientCode, join.Mapping.BillingEntityCode);
                result.Errors.AddRange(groupByClient.SelectMany(g => NoTypeForBillingEntity(g.Index)));
            }

            var otb = await _mediator.Send(new OriginClientQuery(groupByClient.Key), cancellationToken).ConfigureAwait(false);

            if (otb == null)
            {
                _logger.OriginClientNotFound(groupByClient.Key);
                result.Errors.AddRange(groupByClient.SelectMany(g => NoOrigin(g.Index)));
                continue;
            }

            batch.AddRange(from g in groupByClient
                           join e in entities
                           on new { g.Mapping.ClientCode, g.Mapping.BillingEntityCode } equals new { e.ClientCode, e.BillingEntityCode }
                           select new BatchSolveRequest
                           {
                               CorrelationId = g.Lookup.CorrelationID,
                               OriginCode = otb.Origin.OriginCode,
                               ClientCode = g.Mapping.ClientCode,
                               BillingEntityCode = g.Mapping.BillingEntityCode,
                               TaxId = g.Lookup.TaxID,
                               ProviderName = g.Lookup.ProviderName,
                               ProviderAddress1 = g.Lookup.ProviderAddress1,
                               ProviderType = e.Type.Name,
                               ZipCode = g.Lookup.ZipCode,
                               Amount = g.Lookup.Amount
                           });
        }

        foreach (var lookup in request.Batch.Select((item, index) => new { item, index }).ExceptBy(batch.Select(r => r.CorrelationId), i => i.item.CorrelationID))
        {
            result.Errors.AddRange(NoConfig(lookup.index));
        }

        if (!result.IsValid)
        {
            throw new NoPrsConfigurationValidationException(result.Errors);
        }

        return batch;
    }

    private static IEnumerable<ValidationFailure> NoTypeForClient(int index)
        => from k in new[] { nameof(PreferenceLookupDto.PayerID) }
           select new ValidationFailure($"{PreferenceLookupDtoList.ValidationPropertyName}[{index}].{k}", "Internal Error - Provider Type not found");

    private static IEnumerable<ValidationFailure> NoTypeForBillingEntity(int index)
        => from k in new[] { nameof(PreferenceLookupDto.PayerID), nameof(PreferenceLookupDto.SubPayerID) }
           select new ValidationFailure($"{PreferenceLookupDtoList.ValidationPropertyName}[{index}].{k}", "Internal Error - Provider Type not found");

    private static IEnumerable<ValidationFailure> NoOrigin(int index)
        => from k in new[] { nameof(PreferenceLookupDto.PayerID), nameof(PreferenceLookupDto.SubPayerID) }
           select new ValidationFailure($"{PreferenceLookupDtoList.ValidationPropertyName}[{index}].{k}", "Internal Error - Origin not found");

    private static IEnumerable<ValidationFailure> NoConfig(int index)
        => from k in new[] { nameof(PreferenceLookupDto.PayerID), nameof(PreferenceLookupDto.SubPayerID) }
           select new ValidationFailure($"{PreferenceLookupDtoList.ValidationPropertyName}[{index}].{k}", "Internal Error - Configuration not found");
}

internal static partial class Log
{
    internal static IDisposable ClientScope(this ILogger logger, string clientCode)
        => logger.BeginScope(new Dictionary<string, object>{{"ClientCode", clientCode }});

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "BillingEntities not found for client '{ClientCode}'",
        SkipEnabledCheck = true)]
    public static partial void BillingEntitiesNotFoundForClient(this ILogger logger, string clientCode);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "BillingEntities not found for client '{ClientCode}' and billing entity '{BillingEntityCode}'",
        SkipEnabledCheck = true)]
    public static partial void BillingEntitiesNotFoundForClientEntity(this ILogger logger, string clientCode, string billingEntityCode);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "OriginClient not found for '{ClientCode}'",
        SkipEnabledCheck = true)]
    public static partial void OriginClientNotFound(this ILogger logger, string clientCode);
}
