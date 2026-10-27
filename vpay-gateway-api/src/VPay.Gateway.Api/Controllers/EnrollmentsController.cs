using MediatR;
using Microsoft.AspNetCore.Mvc;
using Refit;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common.Enrollments;
using VPay.Gateway.Common.PlatformConfig;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "GuidewireEnrollment")]
public class EnrollmentsController : ControllerBase
{
    private readonly IUserInfo _userInfo;
    private readonly IMediator _mediator;
    private readonly IPlatformConfigApi _platformConfigApi;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(
        IUserInfo userInfo,
        IMediator mediator,
        IPlatformConfigApi platformConfigApi,
        ILogger<EnrollmentsController> logger)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _platformConfigApi = platformConfigApi ?? throw new ArgumentNullException(nameof(platformConfigApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create an enrollment.
    /// </summary>
    /// <param name="entity">The entity containing the data for the enrollment request.</param>
    /// <param name="token">A token that can be used to cancel the work.</param>
    /// <response code="202">If the enrollment request was accepted.</response>
    /// <response code="400">If the entity contains invalid data.</response>
    /// <response code="409">If there is already an active enrollment for the member.</response>
    [HttpPost]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status409Conflict)]
    public async Task<ActionResult> Post(CreateEnrollmentDto entity, CancellationToken token = default)
    {
        var clientCode = string.IsNullOrWhiteSpace(entity.ClientCode)
            ? _userInfo.ClientCodes.FirstOrDefault()
            : entity.ClientCode;

        if (clientCode == "*" || string.IsNullOrWhiteSpace(clientCode))
        {
            ModelState.AddModelError(nameof(entity.ClientCode), "You must specify a client code");
            return BadRequest(ModelState);
        }

        // translate the funding account code into the billing entity code
        var billingEntityDetails = await GetBillingEntityByFundingAccountCode(entity, clientCode, token);
        if (string.IsNullOrWhiteSpace(billingEntityDetails?.BillingEntityCode))
        {
            ModelState.AddModelError(nameof(entity.FundingAccountCode), "Invalid funding account code");
            return BadRequest(ModelState);
        }

        using var scope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["ClientCode"] = clientCode,
                ["BillingEntityCode"] = billingEntityDetails.BillingEntityCode,
                ["MemberId"] = entity.MemberId
            });

        var command = new CreateEnrollmentCommand(
            clientCode,
            billingEntityDetails.BillingEntityCode,
            entity.PayeeName,
            entity.PayeeEmail,
            entity.MemberId,
            entity.PolicyNumber,
            entity.CellPhoneNumber);

        try
        {
            await _mediator.Send(command, token);
            return Accepted();
        }
        catch (EnrollmentInProgressException ex)
        {
            ModelState.AddModelError(string.Empty, $"An enrollment is already in progress for {ex.MemberId}");
            return Conflict(ModelState);
        }
        catch (NotAuthorizedException)
        {
            return Forbid();
        }
    }

    private async Task<OtbBillingEntityDetailDto> GetBillingEntityByFundingAccountCode(
        CreateEnrollmentDto entity,
        string clientCode,
        CancellationToken token)
    {
        try
        {
            return await _platformConfigApi.GetOtbBillingEntityDetails(
                clientCode,
                entity.FundingAccountCode,
                token);
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
