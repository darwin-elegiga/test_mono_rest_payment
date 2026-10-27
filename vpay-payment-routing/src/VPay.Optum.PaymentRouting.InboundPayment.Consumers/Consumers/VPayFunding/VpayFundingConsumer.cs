using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using VPay.MassTransit.Serialization;
using VPay.Optum.PaymentRouting.Common.Serialization;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayFunding
{
    public class VpayFundingConsumer : IConsumer<VpayFunding>
    {
        private readonly IBus _bus;
        private readonly ILogger<VpayFundingConsumer> _logger;

        public VpayFundingConsumer(
            IBus bus,
            ILogger<VpayFundingConsumer> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        [ExcludeFromCodeCoverage]
        public async Task Consume(ConsumeContext<VpayFunding> context)
        {
            await Consume(context.Message, context.CancellationToken).ConfigureAwait(false);
        }

        public async Task Consume(VpayFunding message, CancellationToken cancellationToken)
        {
            var msg = $"Funding | " +
                      $"PayerID: {message.payer835ID}, " +
                      $"BatchID {message.batchID}, " +
                      $"TotCount: {message.totalPaymentCount}, " +
                      $"TotAmt: {message.totalPaymentAmount.ToDecimal()}";
            _logger.LogInformation("Consumed VpayFunding BATCH message {Text}.", msg);

            var amount = message.totalPaymentAmount.ToDecimal();
            var summary = new Common.Serialization.PaymentSummary()
            {
                Payer835Id = message.payer835ID,
                BatchId = message.batchID,
                TotalPaymentCount = message.totalPaymentCount.HasValue ? message.totalPaymentCount.Value.ToString() : "0",
                TotalPaymentAmount = amount.HasValue ? amount.Value.ToString("N2") : "0.00"
            };

            await _bus.Publish(summary, cancellationToken).ConfigureAwait(false);
        }
    }
}
