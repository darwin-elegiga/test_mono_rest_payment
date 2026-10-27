using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using VPay.MassTransit.Serialization;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.InboundPayment.Application.Constants;

namespace VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.VPayPayment
{
    public class VpayPaymentConsumer : IConsumer<VpayPayment>
    {
        private readonly IBus _bus;
        private readonly ILogger<VpayPaymentConsumer> _logger;

        public VpayPaymentConsumer(
            IBus bus,
            ILogger<VpayPaymentConsumer> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        [ExcludeFromCodeCoverage]
        public async Task Consume(ConsumeContext<VpayPayment> context)
        {
            await Consume(context.Message, context.CancellationToken).ConfigureAwait(false);
        }

        public async Task Consume(VpayPayment message, CancellationToken cancellationToken)
        {
            var msg = $"Payment | " +
                $"PayerID: {message.payer835ID}, " +
                $"SubPayerID: {message.payerSubpayerID}, " +
                $"BatchID: {message.batchID}, " +
                $"PaymentNbr: {message.unconsolPaymentNb}, " +
                $"PaymentAmt: {message.uconslPaymentAmount.ToDecimal()}";
            if (message.transactionType == TransactionTypes.Eof)
            {
                _logger.LogInformation("Consumed VpayPayment EOF message {Text}.", msg);
            }
            else
            {
                _logger.LogInformation("Consumed VpayPayment PAY message {Text}.", msg);
            }

            var payAmount = message.uconslPaymentAmount.ToDecimal();
            var payRecord = new Common.Serialization.PaymentRecord()
            {
                BatchId = message.batchID,
                Payer835Id = message.payer835ID,
                PayerSubpayerId = message.payerSubpayerID,
                FileProcId = message.fileProcID?.ToString(),
                PayerAlias = message.payerAlias,
                TransactionType = message.transactionType,
                ProviderTin = message.provTIN,
                ZipCode = message.zipCode,
                UnconsolidatedPaymentNbr = message.unconsolPaymentNb,
                UnconsolidatedPaymentAmount = payAmount.HasValue ? payAmount.Value.ToString() : "0:00",
                BlobStorageUri = message.blobStorageURI,
                PaymentClob = message.paymentClob
            };

            await _bus.Publish(payRecord, cancellationToken).ConfigureAwait(false);
        }
    }
}
