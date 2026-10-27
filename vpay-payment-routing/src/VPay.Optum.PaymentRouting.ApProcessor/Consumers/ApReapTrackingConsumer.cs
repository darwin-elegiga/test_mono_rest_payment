using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.Common.Helpers;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.Data.SmartEnums;
using VPay.Optum.PaymentRouting.Data.Sql.ApReap.Commands;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    public class ApReapTrackingConsumer : IConsumer<ApReapTracking>
    {
        private readonly IMediator _mediator;
        private readonly IUserInfo _userInfo;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ApReapTrackingConsumer> _logger;
        private readonly string _modifiedBy;

        public ApReapTrackingConsumer(
            IMediator mediator,
            IUserInfo userInfo,
            IPublishEndpoint publishEndpoint,
            ILogger<ApReapTrackingConsumer> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modifiedBy = _userInfo.GetCurrentUserName();
        }

        public async Task Consume(ConsumeContext<ApReapTracking> context)
        {
            _logger.LogInformation("Updating AP/REAP tracking status for TrackingId: {ApMessageTrackingId} with Status: {Status}",
                context.Message.ApMessageTrackingId, context.Message.ApMessageTrackingStatus);

            long apTrackingId = context.Message.ApMessageTrackingId;
            try
            {
                var apStatus = ApMessageTrackingStatus.FromName(context.Message.ApMessageTrackingStatus);

                // Update the AP/REAP Message Tracking status.
                await UpdateTrackingStatus(apTrackingId, apStatus).ConfigureAwait(false);

                var payment = new ApReapPayment
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

                // Queue to update the Inbound Payment status and then build the 835 files.
                await _publishEndpoint.Publish(payment, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR updating AP/REAP tracking status for TrackingId: {ApMessageTrackingId} with Status: {Status}",
                    context.Message.ApMessageTrackingId, context.Message.ApMessageTrackingStatus);
                throw;
            }
        }

        /// <summary>
        /// Updates the message tracking status.
        /// </summary>
        /// <param name="apMessageTrackingId">The tracking Id of the message.</param>
        /// <param name="apMessageTrackingStatus">The new status for the message.</param>
        /// <param name="cancellationToken">A token to cancel the work.</param>
        /// <returns></returns>
        public async Task UpdateTrackingStatus(long apMessageTrackingId, ApMessageTrackingStatus apMessageTrackingStatus, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(new UpdateApMessageTrackingStatus.Command
            {
                ApMessageTrackingId = apMessageTrackingId,
                Status = apMessageTrackingStatus,
                ModifiedBy = _modifiedBy
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
