using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Api.MvcCustomizations;
using VPay.Gateway.Application;
using VPay.Gateway.Common.PlatformConfig;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PreferencesController> _logger;

    public PreferencesController(IMediator mediator, ILogger<PreferencesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get point of contact for a given transaction
    /// </summary>
    /// <param name="entity">The entity containing the data for the transaction.</param>
    /// <param name="token"></param>
    /// <response code="200">If the PoC request was accepted.</response>
    /// <response code="400">If the entity contains invalid data.</response>
    [HttpPost("prs-decision-test")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    public async Task<ActionResult<PrsDecisionResult>> PrsDecisionTestAsync(GetPocDto entity, CancellationToken token = default)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["SubPayerId"] = entity.BillingEntityId,
            ["TaxId"] = entity.TaxId
        }))
        {
            var optumSubPayer = await _mediator.Send(new OptumSubPayerQuery(entity.BillingEntityId), token).ConfigureAwait(false);
            if (optumSubPayer == null)
            {
                _logger.LogWarning("Optum Sub Payer not found for {SubPayerId}", entity.BillingEntityId.Replace(Environment.NewLine, string.Empty));
                return Ok(new PrsDecisionResult { Result = PrsDecisionEnum.Optum });
            }

            _logger.LogInformation("Calculating PRS Decision for {SolveParam}", ToFormattedString(entity, optumSubPayer).Replace(Environment.NewLine, string.Empty));

            var command = new PrsDecisionCommand(
                optumSubPayer.BillingEntityCode,
                entity.TaxId,
                entity.ZipCode,
                entity.Amount,
                entity.Date,
                optumSubPayer.ClientCode);

            var response = await _mediator.Send(command, token);

            _logger.LogInformation("Calculated result: {ResultCode}", response.Result);

            return response.Result switch
            {
                "OPTUM" => Ok(new PrsDecisionResult { Result = PrsDecisionEnum.Optum }),
                "VPAY" => Ok(new PrsDecisionResult { Result = PrsDecisionEnum.VPay }),
                _ => BadRequest($"Unexpected result: {response.Result}")
            };
        }
    }

    /// <summary>
    /// Get point of contact for a given transaction
    /// </summary>
    /// <param name="entity">The entity containing the data for the transaction.</param>
    /// <param name="token"></param>
    /// <response code="200">If the PoC request was accepted.</response>
    /// <response code="400">If the entity contains invalid data.</response>
    [HttpPost("prs-decision")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    public Task<ActionResult<PrsDecisionResult>> PrsDecision(GetPocDto entity, CancellationToken token = default)
    {
        return PrsDecisionTestAsync(entity, token);
        //if (long.TryParse(entity.TaxId, out var taxId) && taxId < 500000001)
        //{
        //    return Ok(new PrsDecisionResult { Result = PrsDecisionEnum.Optum });
        //}
        //else
        //{
        //    return Ok(new PrsDecisionResult { Result = PrsDecisionEnum.VPay });
        //}
    }

    /// <summary>
    /// Look up Payment Type Code and Distribution Method preferences.
    /// </summary>
    /// <param name="requests">The list of preferencing lookups.</param>
    /// <param name="token">A token to cancel the request.</param>
    /// <returns>The list of preferred Payment Type Code and Distribution Method for each lookup request.</returns>
    [HttpPost("lookup")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status501NotImplemented)]
    [Authorize(Roles = "PreferencesLookup")]
    [ValidationExceptionFilter]
    public async Task<ActionResult<IEnumerable<PrsPaymentTypeCodeResult>>> PreferenceLookup(
        PreferenceLookupDtoList requests,
        CancellationToken token = default)
    {
        var mappings = await _mediator.Send(
            new Application.BatchPrsMapping.Query(requests), token).ConfigureAwait(false);

        var solveRequests = await _mediator.Send(
            new Application.BatchSolveRequestBuilder.Query(requests, mappings), token).ConfigureAwait(false);

        var solveResponse = await _mediator.Send(
            new Application.BatchSolve.Query(solveRequests), token).ConfigureAwait(false);

        return Ok(solveResponse.Select(r => new PrsPaymentTypeCodeResult
        {
            CorrelationID = r.CorrelationId,
            PaymentTypeCode = r.PaymentTypeCode,
            DistributionDescription = r.DistributionDescription
        }));
    }

    private static string ToFormattedString(GetPocDto entity, OptumSubPayerDto optumSubPayer)
    {
        return $@"{nameof(OptumSubPayerDto.ClientCode)}={optumSubPayer.ClientCode?.Trim()},
{nameof(OptumSubPayerDto.BillingEntityCode)}={optumSubPayer.BillingEntityCode?.Trim()},
{nameof(GetPocDto.TaxId)}={entity.TaxId?.Trim()},
{nameof(GetPocDto.ZipCode)}={entity.ZipCode?.Trim()},
{nameof(GetPocDto.Amount)}={entity.Amount},
{nameof(GetPocDto.Date)}={entity.Date?.ToShortDateString()}";
    }
}
