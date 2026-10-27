using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using VPay.Optum.PaymentRouting.ApProcessor.Constants;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;

namespace VPay.Optum.PaymentRouting.ApProcessor.Consumers
{
    public class ApReapOutboundFileConsumer : IConsumer<ApReapOutboundFile>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ApReapOutboundFileConsumer> _logger;

        public ApReapOutboundFileConsumer(
            IPublishEndpoint publishEndpoint,
            ILogger<ApReapOutboundFileConsumer> logger)
        {
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Consume(ConsumeContext<ApReapOutboundFile> context)
        {
            var apMessageTrackingId = context.Message.ApMessageTrackingId;
            var inboundPaymentId = context.Message.InboundPaymentId;
            var clientCode = context.Message.ClientCode;
            var messageType = context.Message.ApMessageType;
            var routingType = context.Message.ApRoutingType;

            try
            {
                if (routingType == RoutingTypes.Opach)
                {
                    // Consolidated 835 file.
                    // (Done at PRF File Builder for Optum ACH.)
                }

                else if (routingType == RoutingTypes.FinSeg)
                {
                    // FinSeg 835.
                    _logger.LogInformation("Queuing message to build FINSEG 835 file(s) for TrackingId {ApMessageTrackingId} and PaymentId: {InboundPaymentId} - ClientCode: {ClientCode} | AP/REAP: {MessageType} | RouteType: {RoutingType}",
                        apMessageTrackingId, inboundPaymentId, clientCode, messageType, routingType);

                    // Queue to build FinSeg 835 file and FinSeg TXT summary file.
                    await _publishEndpoint.Publish(new BuildOutboundFinSeg(apMessageTrackingId, clientCode)).ConfigureAwait(false);
                }

                else if (routingType == RoutingTypes.Azure)
                {
                    if (messageType == MessageTypes.Ap)
                    {
                        // Consolidated 835 file.
                        _logger.LogInformation("Queuing message to build CONSOL 835 file(s) for TrackingId {ApMessageTrackingId} and PaymentId: {InboundPaymentId} - ClientCode: {ClientCode} | AP/REAP: {MessageType} | RouteType: {RoutingType}",
                            apMessageTrackingId, inboundPaymentId, clientCode, messageType, routingType);

                        // Queue to build Consolidated 835 file.
                        await _publishEndpoint.Publish(new BuildOutbound835(apMessageTrackingId, clientCode)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR queuing message to build 835 file(s) for TrackingId {ApMessageTrackingId} and PaymentId: {InboundPaymentId} - ClientCode: {ClientCode} | AP/REAP: {MessageType} | RouteType: {RoutingType}",
                    apMessageTrackingId, inboundPaymentId, clientCode, messageType, routingType);
                throw;
            }
        }
    }
}
