using MassTransit;
using MediatR;
using VPay.Gateway.Common.Commands;
using VPay.Gateway.Common.Guidewire;
using VPay.Gateway.Common.Guidewire.Models;
using VPay.Gateway.Common.Guidewire.OptumAPI;
using VPay.Gateway.Common.Models;
using VPay.Gateway.Common.PlatformConfig;

namespace VPay.Gateway.ClientProcessor.Rabbit;

public class SendGuidewireMessageConsumer : IConsumer<SendGuidewireMessage>
{
    private readonly ILogger<SendGuidewireMessageConsumer> _logger;
    private readonly IMediator _mediator;
    private readonly IGuidewireServiceProxy _guidewireService;

    public SendGuidewireMessageConsumer(
        ILogger<SendGuidewireMessageConsumer> logger,
        IMediator mediator,
        IGuidewireServiceProxy guidewireService)
    {
        _logger = logger;
        _mediator = mediator;
        _guidewireService = guidewireService;
    }

    public async Task Consume(ConsumeContext<SendGuidewireMessage> context)
    {
        var command = context.Message;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ClientCode"] = command.ClientCode,
            ["ClaimNumber"] = command.ClaimNumber,
            ["PayeeId"] = command.PayeeId,
            ["PaymentId"] = command.PaymentId
        }))
        {
            _logger.LogInformation("Processing GW message.");

            var externalClientConfiguration = await _mediator.Send(new ExternalClientConfigurationQuery(command.ClientCode, DateTime.Today)).ConfigureAwait(false);
            if (externalClientConfiguration == null)
            {
                _logger.LogError("Cannot find a configuration for {ClientCode}", command.ClientCode);
                throw new InvalidOperationException($"Cannot find a configuration for {command.ClientCode}");
            }

            NoteDetails note = command.Note == null ? null : new NoteDetails
            {
                Body = command.Note.Body,
                ClaimNumber = command.ClaimNumber,
                PayeeID = command.PayeeId,
                PaymentID = command.PaymentId,
                Subject = command.Note.Subject,
                Topic = Enum.Parse<NoteTopicType>(command.Note.Topic),
                TopicSpecified = true
            };

            var settings = new GuidewireSettings
            {
                Url = externalClientConfiguration.Url,
                UserName = externalClientConfiguration.Username,
                Password = externalClientConfiguration.Password
            };

            switch (command.CallType)
            {
                case GatewayCallType.EnrollmentStatus:
                    {
                        var payDetail = new OptumEnrollmentUpdateDetails
                        {
                            ClaimNumber = command.ClaimNumber,
                            PayeeID = command.PayeeId,
                            Status = command.Status
                        };

                        await _guidewireService.UpdateEnrollmentDetails(payDetail, settings).ConfigureAwait(false);
                        break;
                    }
                default:
                    {
                        await _guidewireService.CreateNote(note, settings).ConfigureAwait(false);
                        break;
                    }
            }
        }
    }
}
