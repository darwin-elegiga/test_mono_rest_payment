using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.Application.BatchPrsMapping;

/// <summary>
/// Call <see cref="OptumPayerQuery"/> for batch of <see cref="PreferenceLookupDto"/>
/// </summary>
/// <param name="Batch"></param>
/// <exception cref="FluentValidation.ValidationException" />
public record Query(IEnumerable<PreferenceLookupDto> Batch) : IRequest<List<OptumSubPayerDto>>;

public class Handler : IRequestHandler<Query, List<OptumSubPayerDto>>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;

    public Handler(ILogger<Handler> logger, IMediator mediator, IUserInfo userInfo)
    {
        _logger = logger;
        _mediator = mediator;
        _userInfo = userInfo;
    }

    public async Task<List<OptumSubPayerDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var result = new ValidationResult();

        var mappings = new List<OptumSubPayerDto>();

        foreach (var groupByPayer in request.Batch.Select((Item, Index) => (Item, Index)).GroupBy(l => l.Item.PayerID))
        {
            using var _ = _logger.PayerScope(groupByPayer.Key);

            var groupMappings = await _mediator.Send(new OptumPayerQuery(groupByPayer.Key), cancellationToken).ConfigureAwait(false);

            if (groupMappings == null || groupMappings.Count < 1)
            {
                _logger.MappingNotFoundForPayer(groupByPayer.Key);
                result.Errors.AddRange(groupByPayer.Select(PayerNotFound));
                continue;
            }

            foreach (var groupBySubPayer in groupByPayer.GroupBy(c => c.Item.SubPayerID))
            {
                using var __ = _logger.SubPayerScope(groupByPayer.Key);

                var mapping = groupMappings.Find(i => i.OptumSubPayorId == groupBySubPayer.Key);

                if (mapping == null)
                {
                    _logger.MappingNotFoundForSubPayer(groupByPayer.Key, groupBySubPayer.Key);
                    result.Errors.AddRange(groupBySubPayer.Select(SubPayerNotFound));
                    continue;
                }

                if (!_userInfo.CanAccess(mapping.ClientCode, mapping.BillingEntityCode))
                {
                    _logger.MappingNotAccessible(_userInfo.DisplayName, mapping.ClientCode, mapping.BillingEntityCode);
                    result.Errors.AddRange(groupBySubPayer.SelectMany(MappingNotFound));
                }

                mappings.Add(mapping);
            }
        }

        if (!result.IsValid)
        {
            throw new FluentValidation.ValidationException(result.Errors);
        }

        return mappings;
    }

    private static ValidationFailure PayerNotFound((PreferenceLookupDto Dto, int Index) p)
        => new($"{PreferenceLookupDtoList.ValidationPropertyName}[{p.Index}].{nameof(PreferenceLookupDto.PayerID)}",
            $"Payer {p.Dto.PayerID} not found");

    private static ValidationFailure SubPayerNotFound((PreferenceLookupDto Dto, int Index) p)
        => new($"{PreferenceLookupDtoList.ValidationPropertyName}[{p.Index}].{nameof(PreferenceLookupDto.SubPayerID)}",
            $"SubPayer {p.Dto.SubPayerID} not found");

    private static ValidationFailure[] MappingNotFound((PreferenceLookupDto Dto, int Index) p)
        => new[] { PayerNotFound(p), SubPayerNotFound(p) };
}

internal static partial class Log
{
    internal static IDisposable PayerScope(this ILogger logger, string payerId)
        => logger.BeginScope(new Dictionary<string, object>
                {
                    {"PayerId", payerId },
                });

    internal static IDisposable SubPayerScope(this ILogger logger, string subPayerId)
        => logger.BeginScope(new Dictionary<string, object>
                {
                    {"SubPayerId", subPayerId },
                });

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "No PRS Mapping found for payer '{PayerId}'",
        SkipEnabledCheck = true)]
    public static partial void MappingNotFoundForPayer(this ILogger logger, string payerId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "No PRS Mapping found for payer '{PayerId}' and sub payer '{SubPayerId}'",
        SkipEnabledCheck = true)]
    public static partial void MappingNotFoundForSubPayer(this ILogger logger, string payerId, string subPayerId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "PRS Mapping - SECURITY - Client '{DisplayName}' tried to view PRS Mapping for Client '{ClientCode}' and Billing Entity '{BillingEntityCode}' without the respective access",
        SkipEnabledCheck = true)]
    public static partial void MappingNotAccessible(this ILogger logger, string displayName, string clientCode, string billingEntityCode);
}
