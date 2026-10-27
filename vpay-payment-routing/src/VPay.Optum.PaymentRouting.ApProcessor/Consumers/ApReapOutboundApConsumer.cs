using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    public class ApReapOutboundApConsumer : IConsumer<ApReapOutboundAzure>
    {
        private readonly IMediator _mediator;
        private readonly IUserInfo _userInfo;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ApReapOutboundApConsumer> _logger;
        private readonly string _modifiedBy;

        public ApReapOutboundApConsumer(
            IMediator mediator,
            IUserInfo userInfo,
            IPublishEndpoint publishEndpoint,
            ILogger<ApReapOutboundApConsumer> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modifiedBy = _userInfo.GetCurrentUserName();
        }

        public async Task Consume(ConsumeContext<ApReapOutboundAzure> context)
        {
            var transactionId = context.Message.TransactionId;
            var clientCode = context.Message.ClientCode;
            var beCode = context.Message.BillingEntityCode;
            var messageType = context.Message.ApMessageType;
            var routingType = context.Message.ApRoutingType;
            var userKey = context.Message.UnConsolPayNumber;
            var message = context.Message;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["ClientCode"] = clientCode,
                ["BillingEntityCode"] = beCode,
                ["UserKey"] = userKey,
                ["MessageType"] = messageType,
                ["RoutingType"] = routingType,
                ["Action"] = "InsertApMessage"
            }))
            {
                try
                {
                    // Store the message in the database for tracking and auditing purposes,
                    // and to support resending Azure messages if needed.

                    // Insert ApMessage new record.
                    var command = new InsertApMessage.Command
                    {
                        TransactionId = message.TransactionId,
                        ClientCode = message.ClientCode,
                        BillingEntityCode = message.BillingEntityCode,
                        ApMessageType = message.ApMessageType,
                        ApRoutingType = message.ApRoutingType,
                        IsOptumEnrolled = message.IsOptumEnrolled,
                        // Avro Fields
                        PayerId = message.PayerId,
                        SubPayerId = message.SubPayerId,
                        PaymentSource = message.PaymentSource,
                        PaymentType = message.PaymentType,
                        ProviderNumber = message.ProviderNumber,
                        ProviderName = message.ProviderName,
                        ProviderNpiNumber = message.ProviderNpiNumber,
                        UnConsolPayNumber = message.UnConsolPayNumber,
                        UnConsolPayAmount = message.UnConsolPayAmount,
                        DisplayConsolPayNumber = message.DisplayConsolPayNumber,
                        ConsolAmount = message.ConsolAmount,
                        PayMethodCode = message.PayMethodCode,
                        SettlementDate = message.SettlementDate,
                        CheckNumber = message.CheckNumber,
                        ReturnReasonCode = message.ReturnReasonCode,
                        RevisedMethodCode = message.RevisedMethodCode,
                        ReOriginatedDate = message.ReOriginatedDate,
                        CreatedBy = _modifiedBy,
                    };

                    var response = await _mediator.Send(command, context.CancellationToken).ConfigureAwait(false);

                    // Notify result so failures aren't silently ignored.
                    if (response.IsSuccess)
                    {
                        _logger.LogInformation("Inserted ApMessage for payment {UserKey}", userKey);
                    }
                    else
                    {
                        _logger.LogError("Failed to insert ApMessage for payment {UserKey}: {Errors}", userKey, string.Join(" | ", response.Errors));
                        throw new InvalidOperationException($"Failed to insert ApMessage for payment {userKey}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ERROR inserting AP/REAP message for payment {UserKey} and message type '{MessageType}'",
                        userKey, context.Message.ApMessageType);
                    throw;
                }
            }
        }
    }
}
