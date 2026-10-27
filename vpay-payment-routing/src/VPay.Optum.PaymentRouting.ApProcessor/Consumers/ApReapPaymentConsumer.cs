using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.InboundPayment.Commands;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    public class ApReapPaymentConsumer : IConsumer<ApReapPayment>
    {
        private readonly IMediator _mediator;
        private readonly IUserInfo _userInfo;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ApReapPaymentConsumer> _logger;
        private readonly string _modifiedBy;

        public ApReapPaymentConsumer(
            IMediator mediator,
            IUserInfo userInfo,
            IPublishEndpoint publishEndpoint,
            ILogger<ApReapPaymentConsumer> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modifiedBy = _userInfo.GetCurrentUserName();
        }

        public async Task Consume(ConsumeContext<ApReapPayment> context)
        {
            _logger.LogInformation("Updating AP/REAP payment status for PaymentId: {InboundPaymentId} with Status: {Status}",
                context.Message.InboundPaymentId, context.Message.InboundPaymentStatus);

            long inboundPaymentId = context.Message.InboundPaymentId;
            try
            {
                var inboundPaymentStatus = PaymentStatus.FromName(context.Message.InboundPaymentStatus);

                // Update the Inbound Payment status.
                await UpdatePaymentStatus(inboundPaymentId, inboundPaymentStatus).ConfigureAwait(false);

                var outboundFile = new ApReapOutboundFile
                {
                    ClientCode = context.Message.ClientCode,
                    BillingEntityCode = context.Message.BillingEntityCode,
                    ApMessageType = context.Message.ApMessageType,
                    ApRoutingType = context.Message.ApRoutingType,
                    IsOptumEnrolled = context.Message.IsOptumEnrolled,
                    InboundPaymentId = context.Message.InboundPaymentId,
                    InboundPaymentStatus = context.Message.InboundPaymentStatus,
                    ApMessageTrackingId = context.Message.ApMessageTrackingId,
                    ApMessageTrackingStatus = context.Message.ApMessageTrackingStatus
                };

                // Queue to build the appropriate 835 file(s) based on routing type.
                await _publishEndpoint.Publish(outboundFile, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR updating AP/REAP payment status for PaymentId: {InboundPaymentId} with Status: {Status}",
                    context.Message.InboundPaymentId, context.Message.InboundPaymentStatus);
                throw;
            }
        }

        /// <summary>
        /// Updates the inbound payment status.
        /// </summary>
        /// <param name="inboundPaymentId">The payment Id of the payment.</param>
        /// <param name="inboundPaymentStatus">The new status for the message.</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns></returns>
        private async Task UpdatePaymentStatus(long inboundPaymentId, PaymentStatus inboundPaymentStatus, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(new UpdateInboundPaymentStatusSql.Command
            {
                InboundPaymentId = inboundPaymentId,
                Status = inboundPaymentStatus,
                ModifiedBy = _modifiedBy
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
