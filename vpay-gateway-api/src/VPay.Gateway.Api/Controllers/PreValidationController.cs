using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Api.MvcCustomizations;
using VPay.Gateway.Application;
using VPay.Gateway.Common.MemberClaims;
using VPay.Gateway.Common.PlatformConfig;
using VPay.Gateway.Common.Preferencing.Models;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PreValidationController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<PreValidationController> _logger;

    public PreValidationController(IMediator mediator, ILogger<PreValidationController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Look up Payment Type Code and determine if that Payment Type is Monetized.
    /// </summary>
    /// <param name="requestDto">The list of preferencing look-ups.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>If payment type associated is Monetized or non-monetized</returns>
    [HttpPost("pre-validation")]
    [ProducesResponseType(typeof(MonetizationResultDto), Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status501NotImplemented)]
    [Authorize(Roles = "Monetization")]
    [ValidationExceptionFilter]
    public async Task<ActionResult<MonetizationResultDto>> GetMonetizationStatus(
        [FromBody] MonetizationLookupDto requestDto,
        CancellationToken cancellationToken = default)
    {
        if (requestDto == null)
        {
            return Problem(detail: "Monetization - Request body cannot be null", statusCode: 400, title: "Bad Request", type: "https://httpstatuses.io/400");
        }

        var logClientCode = requestDto.ClientCode.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var logBeCode = requestDto.BillingEntityCode.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var logCorrelationId = requestDto.CorrelationID.Replace("\r", string.Empty).Replace("\n", string.Empty);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ClientCode"] = logClientCode,
            ["BillingEntityCode"] = logBeCode,
            ["CorrelationId"] = logCorrelationId,
            ["Action"] = nameof(GetMonetizationStatus)
        }))
        {
            try
            {
                var entities = await _mediator.Send(new BillingEntitiesQuery(requestDto.ClientCode), cancellationToken).ConfigureAwait(false);
                var otb = await _mediator.Send(new OriginClientQuery(requestDto.ClientCode), cancellationToken).ConfigureAwait(false);
                if (otb == null)
                {
                    _logger.LogWarning("Monetization - No OTB information found for {ClientCode}", logClientCode);
                    return Problem(detail: "No OTB information found", statusCode: 404, title: "Not Found", type: "https://httpstatuses.io/404");
                }

                var request = new MonetizationRequest
                {
                    ClientCode = requestDto.ClientCode,
                    CorrelationID = requestDto.CorrelationID,
                    Amount = requestDto.Amount,
                    BillingEntityCode = requestDto.BillingEntityCode,
                    City = requestDto.City,
                    Date = DateTime.Now,
                    OriginCode = otb.Origin.OriginCode,
                    ProviderAddress1 = requestDto.ProviderAddress1,
                    ProviderAddress2 = requestDto.ProviderAddress2,
                    ProviderName = requestDto.ProviderName,
                    State = requestDto.State,
                    TaxId = requestDto.TaxId,
                    ZipCode = requestDto.ZipCode,
                    Plus4 = requestDto.Plus4?.Trim(),
                    ProviderType = entities.FirstOrDefault()?.Type?.Name ?? string.Empty
                };
                var monetizationResult = await _mediator.Send(new GetPreferencingMonetization.Query(request), cancellationToken).ConfigureAwait(false);
                var result = new MonetizationResultDto
                {
                    CorrelationId = monetizationResult.CorrelationId,
                    ValidatedPayee = monetizationResult.ValidatedPayee,
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monetization - Unexpected error for {ClientCode} and {BillingEntity} ", logClientCode, logBeCode);
                return Problem(detail: "Unable to retrieve Monetization information", statusCode: 500, title: "Unexpected Error", type: "https://httpstatuses.io/500");
            }
        }
    }
}
