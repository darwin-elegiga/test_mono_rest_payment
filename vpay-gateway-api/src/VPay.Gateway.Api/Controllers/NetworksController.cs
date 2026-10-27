using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Extensions.Exceptions;
using VPay.Gateway.Api.Models;
using VPay.Gateway.Application;
using VPay.Gateway.Common;
using VPay.Gateway.Common.Preferencing.Enums;
using VPay.Gateway.Common.Preferencing.Models;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace VPay.Gateway.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "PartnerEnrollment")]
public partial class NetworksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnrollmentsController> _logger;

    public NetworksController(
        IMediator mediator,
        ILogger<EnrollmentsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies the configuration status of a network partner to ensure it can receive enrollments.
    /// </summary>
    /// <param name="networkPartner">The Network Partner Name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <response code="200">If a record matching the parameters is found.</response>
    /// <response code="404">If no records matching the parameters can be found.</response>
    /// <response code="400">If the parameters are invalid.</response>
    [HttpGet("{networkPartner}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    public async Task<ActionResult<NetworkResultDto>> GetNetwork(
        [Required(AllowEmptyStrings = false)][MaxLength(50)] string networkPartner,
        CancellationToken cancellationToken = default)
    {
        NetworkResultDto result = new()
        {
            NetworkPartner = networkPartner
        };

        if (string.IsNullOrWhiteSpace(networkPartner))
        {
            return BadRequest("Network Partner Name Or Network Id must be provided and cannot be empty.");
        }

        try
        {
            // Verify network 
            GetNetworkQuery networkQuery = new(networkPartner);
            NetworkDto network = await _mediator
                .Send(networkQuery, cancellationToken)
                .ConfigureAwait(false);

            // Verify network exists.
            if (network == null)
            {
                return NotFound();
            }

            // Verify network status.
            if (network.ActiveStatus == NetworkEnrollmentActiveStatus.Active.ToMember())
            {
                result.NetworkConfigStatus = "OK";
                result.NetworkActiveStatus = "Active";
            }
            else
            {
                result.NetworkConfigStatus = "OK";
                result.NetworkActiveStatus = "Inactive";
            }

            return Ok(result);
        }
        catch (Exception)
        {
            result.NetworkConfigStatus = "Error";
            result.NetworkActiveStatus = "Unknown";
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Verifies the configuration status of a network payor enrollment.
    /// </summary>
    /// <param name="networkPartner">The Network Partner Name.</param>
    /// <param name="clientCode">The client code.</param>
    /// <param name="billingEntityCode">The billing entity code.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <response code="200">If a record matching the parameters is found.</response>
    /// <response code="404">If no records matching the parameters can be found.</response>
    /// <response code="400">If the parameters are invalid.</response>
    [HttpGet("{networkPartner}/clients/{clientCode}/enrollments/{billingEntityCode}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    public async Task<ActionResult<NetworkResultDto>> GetNetworkPayorEnrollment(
        [Required(AllowEmptyStrings = false)][MaxLength(50)] string networkPartner,
        [Required(AllowEmptyStrings = false)][MaxLength(10)] string clientCode,
        [Required(AllowEmptyStrings = false)][MaxLength(20)] string billingEntityCode,
        CancellationToken cancellationToken = default)
    {
        NetworkResultDto result = new()
        {
            NetworkPartner = networkPartner
        };

        if (string.IsNullOrWhiteSpace(networkPartner))
        {
            return BadRequest("Network Partner Name Or Network Id must be provided and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(clientCode))
        {
            return BadRequest("Client Code must be provided and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(billingEntityCode))
        {
            return BadRequest("Billing Entity Code must be provided and cannot be empty.");
        }

        try
        {
            // Verify network payor enrollment
            GetNetworkPayorEnrollmentQuery enrollmentQuery = new(networkPartner, clientCode, billingEntityCode);
            NetworkPayorEnrollmentDto enrollment = await _mediator
                .Send(enrollmentQuery, cancellationToken)
                .ConfigureAwait(false);

            // Verify enrollment exists.
            if (enrollment == null)
            {
                return NotFound();
            }

            result.NetworkConfigStatus = "OK";
            result.NetworkActiveStatus = "Active";

            return Ok(result);
        }
        catch (Exception)
        {
            result.NetworkConfigStatus = "Error";
            result.NetworkActiveStatus = "Unknown";
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Verifies the configuration status of a network provider enrollment.
    /// </summary>
    /// <param name="networkPartner">The Network Partner Name.</param>
    /// <param name="providerTin">The provider Tax Id.</param>
    /// <param name="enrollmentType">The provider enrollment type: C2B or B2B.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the work.</param>
    /// <response code="200">If a record matching the parameters is found.</response>
    /// <response code="404">If no records matching the parameters can be found.</response>
    /// <response code="400">If the parameters are invalid.</response>
    [HttpGet("{networkPartner}/providers/{providerTin}/enrollments/{enrollmentType}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    public async Task<ActionResult<NetworkResultDto>> GetNetworkProviderEnrollment(
        [Required(AllowEmptyStrings = false)][MaxLength(50)] string networkPartner,
        [Required(AllowEmptyStrings = false)][MinLength(9)][MaxLength(9)] string providerTin,
        [Required(AllowEmptyStrings = false)][MaxLength(5)] string enrollmentType,
        CancellationToken cancellationToken = default)
    {
        NetworkResultDto result = new()
        {
            NetworkPartner = networkPartner
        };

        if (string.IsNullOrWhiteSpace(networkPartner))
        {
            return BadRequest("Network Partner Name Or Network Id must be provided and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(providerTin))
        {
            return BadRequest("Provider Tax Id must be provided and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(enrollmentType))
        {
            return BadRequest("Enrollment Type must be provided and cannot be empty.");
        }

        try
        {
            // Verify network provider enrollment
            GetNetworkProviderEnrollmentQuery enrollmentQuery = new(networkPartner, providerTin, enrollmentType);
            NetworkProviderEnrollmentDto enrollment = await _mediator
                .Send(enrollmentQuery, cancellationToken)
                .ConfigureAwait(false);

            // Verify enrollment exists.
            if (enrollment == null)
            {
                return NotFound();
            }

            result.NetworkConfigStatus = "OK";
            result.NetworkActiveStatus = "Active";

            return Ok(result);
        }
        catch (Exception)
        {
            result.NetworkConfigStatus = "Error";
            result.NetworkActiveStatus = "Unknown";
            return BadRequest(result);
        }
    }

    /// <summary>
    /// Process a list of provider enrollment requests from a network parter.
    /// </summary>
    /// <param name="entity">The entity containing the data for the enrollment request to save.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="202">If the enrollment request was accepted.</response>
    /// <response code="400">If the entity contains invalid data.</response>
    /// <response code="401">If the user cannot be authorized.</response>
    /// <response code="403">If the user does not have the required role.</response>
    /// <response code="409">If there is already an active enrollment for the member.</response>
    [HttpPost("providers")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status401Unauthorized)]
    [ProducesResponseType(Status403Forbidden)]
    [ProducesResponseType(Status409Conflict)]
    public async Task<ActionResult<NetworkEnrollmentHeaderDto>> EnrollNetworkProviders(
        [FromBody] NetworkEnrollmentRequestDto entity,
        CancellationToken cancellationToken = default)
    {
        string networkPartner = string.Empty;
        string counts = string.Empty;
        try
        {
            var validator = new NetworkEnrollmentRequestDtoValidator();
            await validator.ValidateAndThrowAsync(entity, cancellationToken).ConfigureAwait(false);

            int count = entity.PartnerProviders.Sum(e => e.EnrollmentList.Count());
            counts = $"{entity.Header.Count} enrollments";
            networkPartner = entity.Header.NetworkPartner;

            if (count != entity.Header.Count)
            {
                string errorMessage = $"Expected {entity.Header.Count} records but received {count} records instead.";
                Log.EnrollmentValidationError(_logger, networkPartner, entity.Header.CorrelationId, counts, errorMessage);
                return BadRequest(errorMessage);
            }

            NetworkEnrollmentHeaderDto header = entity.Header;
            if (!networkPartner.Equals(header.NetworkPartner, StringComparison.OrdinalIgnoreCase))
            {
                string errorMessage = $"Expected network '{networkPartner}' but received network '{header.NetworkPartner}' instead.";
                Log.EnrollmentValidationError(_logger, networkPartner, entity.Header.CorrelationId, counts, errorMessage);
                return BadRequest(errorMessage);
            }


            // Verify network 
            var networkQuery = new GetNetworkQuery(networkPartner);
            NetworkDto network = await _mediator
                .Send(networkQuery, cancellationToken)
                .ConfigureAwait(false);

            // Verify network exists.
            if (network == null)
            {
                string errorMessage = $"Network '{networkPartner}' not recognized.";
                Log.EnrollmentValidationError(_logger, networkPartner, entity.Header.CorrelationId, counts, errorMessage);
                return BadRequest(errorMessage);
            }

            // Verify network is active.
            if (network.ActiveStatus != NetworkEnrollmentActiveStatus.Active.ToMember())
            {
                string errorMessage = $"Network '{networkPartner}' not active.";
                Log.EnrollmentValidationError(_logger, networkPartner, entity.Header.CorrelationId, counts, errorMessage);
                return BadRequest(errorMessage);
            }

            // Save enrollment request for processing.
            var request = new NetworkEnrollmentRequest(entity);
            var command = new CreateNetworkEnrollmentCommand(request);

            await _mediator
                .Send(command, cancellationToken)
                .ConfigureAwait(false);

            var response = new NetworkEnrollmentResponse
            {
                CorrelationId = entity.Header.CorrelationId,
                NetworkPartner = entity.Header.NetworkPartner,
                EnrollmentCount = entity.Header.Count,
            };

            Log.EnrollmentAccepted(_logger, networkPartner, entity.Header.CorrelationId, counts);
            return Accepted(response);
        }
        catch (FluentValidation.ValidationException ex)
        {
            Log.EnrollmentValidationError(_logger, networkPartner, entity?.Header?.CorrelationId, counts, ex.Message);
            return BadRequest(ex.Errors);
        }
        catch (NotAuthorizedException ex)
        {
            Log.EnrollmentNotAuthenticated(_logger, ex, networkPartner, entity.Header.CorrelationId, counts);
            return Forbid();
        }
        catch (ConflictException ex)
        {
            Log.EnrollmentConflictError(_logger, ex, networkPartner, entity.Header.CorrelationId, counts);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            Log.EnrollmentOperationError(_logger, ex, networkPartner, entity.Header.CorrelationId, counts);
            return BadRequest(ex.Message);
        }
    }

    public static partial class Log
    {
        [LoggerMessage(
            EventId = 0,
            Level = LogLevel.Information,
            Message = "Network Enrollment Request ACCEPTED [Network = '{NetworkName}', Correlation = '{CorrelationId}', RejectionCode = {RejectionCode}]",
            SkipEnabledCheck = true)]
        public static partial void EnrollmentAccepted(ILogger logger, string networkName, string correlationId, string rejectionCode);

        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "Network Enrollment Request DENIED [Network = '{NetworkName}', Correlation = '{CorrelationId}', RejectionCode = {RejectionCode}] : {ErrorMessage}",
            SkipEnabledCheck = true)]
        public static partial void EnrollmentValidationError(ILogger logger, string networkName, string correlationId, string rejectionCode, string errorMessage);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Error,
            Message = "Network Enrollment Request DENIED [Network = '{NetworkName}', Correlation = '{CorrelationId}', RejectionCode = {RejectionCode}] : Authentication Error",
            SkipEnabledCheck = true)]
        public static partial void EnrollmentNotAuthenticated(ILogger logger, Exception ex, string networkName, string correlationId, string rejectionCode);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Error,
            Message = "Network Enrollment Request DENIED [Network = '{NetworkName}', Correlation = '{CorrelationId}', RejectionCode = {RejectionCode}] : Conflict Error",
            SkipEnabledCheck = true)]
        public static partial void EnrollmentConflictError(ILogger logger, Exception ex, string networkName, string correlationId, string rejectionCode);

        [LoggerMessage(
            EventId = 4,
            Level = LogLevel.Error,
            Message = "Network Enrollment Request DENIED [Network = '{NetworkName}', Correlation = '{CorrelationId}', RejectionCode = {RejectionCode}] : Operation Error",
            SkipEnabledCheck = true)]
        public static partial void EnrollmentOperationError(ILogger logger, Exception ex, string networkName, string correlationId, string rejectionCode);
    }
}
