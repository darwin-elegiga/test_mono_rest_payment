using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Data.Paging;
using VPay.Gateway.Data.Sql.PepClaims;
using VPay.Gateway.Management.Api.Models;
using VPay.Gateway.Management.Application.GuidewirePayments;

namespace VPay.Gateway.Management.Api.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class GuidewireController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMediator _mediator;
    private readonly GuidewireReplaySettings _settings;

    public GuidewireController(
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        IMediator mediator,
        GuidewireReplaySettings settings)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Post a payment message to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the message to send.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("payment-update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuidewireResult>> PostMessage(GuidewirePaymentMessageDto message, CancellationToken cancellationToken = default)
    {
        var status = Enum.Parse<GuidewirePaymentStatus>(message.Status?.ToString(), true);
        try
        {
            var command = new UpdateGuidewirePaymentStatusCommand(message.ClientCode, message.PaymentId, status, message.Note);

            return await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(ex.ParamName, ex.Message);
            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Post a enrollment message to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the message to send.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-update")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostEnrollmentMessage(GuidewireEnrollmentStatusChangeDto message, CancellationToken cancellationToken = default)
    {
        var messageToPublish = _mapper.Map<SendGuidewireMessage>(message);

        await _publishEndpoint.Publish(messageToPublish, x => x.Durable = true, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }

    /// <summary>
    /// Search Guidewire enrollment messages.
    /// </summary>
    /// <param name="message">The payload containing the search filters for the messages to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpGet("enrollment-search")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Pagination<GuidewireEnrollmentStatusDto>>> SearchEnrollmentMessages(
        [Required][FromQuery] GuidewireEnrollmentStatusSearchDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var query = new SearchGuidewireEnrollmentMessages.Query(
                message.ClientCode,
                message.MemberId,
                message.ClaimNumber,
                message.ProcessStatus,
                message.Page,
                message.PageSize);

            var response = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            var dto = _mapper.Map<Pagination<GuidewireEnrollmentStatusDto>>(response);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Search stuck Guidewire enrollment messages left in SENDING status.
    /// The endpoint will locate messages that have been in the SENDING status
    /// for at least 30 minutes (3 minutes in lower environments),
    /// but that are not older than 60 days (30 days in lower environments),
    /// and that have not exceeded the maximum retry count of 6 times.
    /// Use the payload filter to optionally limit the search to only a certain client, member, and/or claim.
    /// Leave the client, member, and claim fields as null to search all messages in SENDING status.
    /// </summary>
    /// <param name="message">The payload containing the search filters for the messages to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpGet("enrollment-search-stuck")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Pagination<GuidewireEnrollmentStatusDto>>> SearchStuckEnrollmentMessages(
        [Required][FromQuery] GuidewireEnrollmentStatusStuckSearchDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var query = new SearchStuckGuidewireEnrollmentMessages.Query(
                message.ClientCode,
                message.MemberId,
                message.ClaimNumber,
                GuidewireEnrollmentMessage.Sending,
                _settings.MinMessageAgeInMinutes,
                _settings.MaxMessageAgeInDays,
                message.Page,
                message.PageSize);

            var response = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            var dto = _mapper.Map<Pagination<GuidewireEnrollmentStatusDto>>(response);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Search stuck Guidewire enrollment messages left in FAILED status.
    /// The endpoint will locate messages that have been in the FAILED status
    /// for at least 30 minutes (3 minutes in lower environments),
    /// but that are not older than 60 days (30 days in lower environments),
    /// and that have not exceeded the maximum retry count of 6 times.
    /// Use the payload filter to optionally limit the search to only a certain client, member, and/or claim.
    /// Leave the client, member, and claim fields as null to search all messages in FAILED status.
    /// </summary>
    /// <param name="message">The payload containing the search filters for the messages to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpGet("enrollment-search-failed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Pagination<GuidewireEnrollmentStatusDto>>> SearchFailedEnrollmentMessages(
        [Required][FromQuery] GuidewireEnrollmentStatusFailedSearchDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var query = new SearchFailedGuidewireEnrollmentMessages.Query(
                message.ClientCode,
                message.MemberId,
                message.ClaimNumber,
                GuidewireEnrollmentMessage.Failed,
                message.Page,
                message.PageSize);

            var response = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            var dto = _mapper.Map<Pagination<GuidewireEnrollmentStatusDto>>(response);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment message processing status of one message from SENDING to COMPLETED status.
    /// </summary>
    /// <param name="message">The payload containing client, member, and claim that identifies
    /// the enrollment message to update.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-complete")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [NonAction]
    public async Task<ActionResult<GuidewireResult>> CompleteEnrollmentMessage(
        [Required][FromBody] GuidewireEnrollmentStatusResetDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ResetGuidewireEnrollmentMessages.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                NewProcessStatus: GuidewireEnrollmentMessage.Completed);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return NoContent();
            }
            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment message processing status of one message from SENDING to SKIPPED status.
    /// </summary>
    /// <param name="message">The payload containing client, member, and claim that identifies
    /// the enrollment message to update.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-skip")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [NonAction]
    public async Task<ActionResult<GuidewireResult>> SkipEnrollmentMessage(
        [Required][FromBody] GuidewireEnrollmentStatusResetDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ResetGuidewireEnrollmentMessages.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                NewProcessStatus: GuidewireEnrollmentMessage.Skipped);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return NoContent();
            }
            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment message processing status of one message from FAILED to SENDING status
    /// and resets the age of the message to allow the message to be replayed. Can be used to reset the age
    /// of a message that is stuck in FAILED status for more than 60 days, so it can be resent to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the required client, member, and claim that identifies
    /// the enrollment message to update.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-reset-failed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuidewireResult>> ResetFailedEnrollmentMessage(
        [Required][FromBody] GuidewireEnrollmentStatusResetDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ResetGuidewireEnrollmentMessages.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                CurrentProcessStatus: GuidewireEnrollmentMessage.Failed,
                NewProcessStatus: GuidewireEnrollmentMessage.Sending);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return NoContent();
            }
            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment message processing status of one message from SCHEDULED to SENDING status
    /// and resets the age of the message to allow the message to be replayed.
    /// Can be used to reset the age of a message that is stuck in SCHEDULED status
    /// for more than 60 days (30 days in lower environments), so it can be resent to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the required client, member, and claim that identifies
    /// the enrollment message to update.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-reset-scheduled")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuidewireResult>> ResetScheduledEnrollmentMessage(
        [Required][FromBody] GuidewireEnrollmentStatusResetDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ResetGuidewireEnrollmentMessages.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                CurrentProcessStatus: GuidewireEnrollmentMessage.Scheduled,
                NewProcessStatus: GuidewireEnrollmentMessage.Sending);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return NoContent();
            }
            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment message processing status of one message from SENDING to SENDING status
    /// and resets the age of the message to allow the message to be replayed.
    /// Can be used to reset the age of a message that is stuck in SENDING status
    /// for more than 60 days (30 days in lower environments), so it can be resent to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the required client, member, and claim that identifies
    /// the enrollment message to update.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-reset-sending")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuidewireResult>> ResetSendingEnrollmentMessage(
        [Required][FromBody] GuidewireEnrollmentStatusResetDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ResetGuidewireEnrollmentMessages.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                CurrentProcessStatus: GuidewireEnrollmentMessage.Sending,
                NewProcessStatus: GuidewireEnrollmentMessage.Sending);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return NoContent();
            }
            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment message retry count to zero for one message stuck in SENDING or SCHEDULED status
    /// and resets the age of the message to allow the message to be replayed.
    /// Can be used to reset the retry count of a message that has already been retried 6 times,
    /// so it can be resent to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the required client, member, and claim that identifies
    /// the enrollment message to update.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-reset-retry-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuidewireResult>> ResetRetryCountForEnrollmentMessages(
        [Required][FromBody] GuidewireEnrollmentStatusResetDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ResetGuidewireEnrollmentMessageCount.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                RetryCount: 0);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                return NoContent();
            }
            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reset the Guidewire enrollment messages processing status of stuck messages from SENDING to SCHEDULED status
    /// and republish the message to the working Guidewire queue so it can be replayed (resent to Guidewire).
    /// The endpoint will process a maximum of 10 messages (3 messages in lower environments) that have been in
    /// the SENDING status for at least 30 minutes (3 minutes in lower environments), starting from the oldest stuck message,
    /// but that are not older than 60 days (30 days in lower environments) (reset aged-out messages to replay them).
    /// Messages that have been retried more than 6 times will not be replayed (reset maxed-out messages to replay them).
    /// Use the payload filter to optionally limit the replay to only a certain client, member, and/or claim.
    /// </summary>
    /// <param name="message">The payload containing additional filters (optional) for the messages to replay.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    [HttpPost("enrollment-replay")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuidewireResult>> ReplayEnrollmentMessages(
        [Required][FromBody] GuidewireEnrollmentStatusReplayDto message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var command = new ReplayGuidewireEnrollmentMessages.Command(
                ClientCode: message.ClientCode,
                MemberId: message.MemberId,
                ClaimId: message.ClaimNumber,
                CurrentProcessStatus: GuidewireEnrollmentMessage.Sending,
                NewProcessStatus: GuidewireEnrollmentMessage.Scheduled,
                MessageCount: _settings.MaxMessageCount,
                MinMessageAgeInMinutes: _settings.MinMessageAgeInMinutes,
                MaxMessageAgeInDays: _settings.MaxMessageAgeInDays);

            var response = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            return Ok(response.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Post a note to Guidewire.
    /// </summary>
    /// <param name="message">The payload containing the message to send.</param>
    /// <param name="cancellationToken"></param>
    [HttpPost("note")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostNote(GuidewireNoteOnlyDto message, CancellationToken cancellationToken = default)
    {
        var messageToPublish = _mapper.Map<SendGuidewireMessage>(message);

        await _publishEndpoint.Publish(messageToPublish, x => x.Durable = true, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }
}
