using MassTransit;
using MediatR;
using VPay.Gateway.Common;
using VPay.Gateway.Data.Sql.Payments;
using VPay.Gateway.PaymentProcessor.Consumer.Logic;

namespace VPay.Gateway.PaymentProcessor.Consumer.Rabbit;

public class ProcessClientPaymentStatusRecordsConsumer : IConsumer<ProcessClientPaymentStatusRecords>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessClientPaymentStatusRecordsConsumer> _logger;

    public ProcessClientPaymentStatusRecordsConsumer(IMediator mediator, ILogger<ProcessClientPaymentStatusRecordsConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessClientPaymentStatusRecords> context)
    {
        var start = StopwatchHelpers.GetTimestamp();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = context.MessageId,
            ["ClientCode"] = context.Message.ClientCode,
            ["PaymentId"] = context.Message.PaymentId
        });

        try
        {
            var completed = await _mediator.Send(new ProcessClientPaymentStatusRecordsCommand(context.Message.ClientCode, context.Message.PaymentId), context.CancellationToken).ConfigureAwait(false);

            if (completed)
            {
                await _mediator.Publish(new RestartOrEndPaymentQueueNotification(context.Message.ClientCode, context.Message.PaymentId, context.Message.RequestTime), context.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _mediator.Send(new PaymentProcessQueueDeleteCommand(context.Message.ClientCode, context.Message.PaymentId), context.CancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("ProcessClientPaymentStatusRecords {Status} in {Elapsed:0.0000} ms", "SUCCESS", start.GetElapsedMilliseconds());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessClientPaymentStatusRecords {Status} in {Elapsed:0.0000} ms", "FAILED", start.GetElapsedMilliseconds());
            throw;
        }
    }
}
