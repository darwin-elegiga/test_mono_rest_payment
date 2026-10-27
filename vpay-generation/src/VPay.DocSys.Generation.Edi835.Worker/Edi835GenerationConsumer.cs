using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.Queuing.Abstractions;

namespace VPay.DocSys.Generation.Edi835.Worker
{
    public class Edi835GenerationConsumer : IQueueConsumer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Edi835GenerationConsumer> _logger;

        public Edi835GenerationConsumer(
            IServiceProvider serviceProvider,
            ILogger<Edi835GenerationConsumer> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Consume(string message, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "QueueMessge", message } }))
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    _logger.LogInformation($"Starting processing Edi835Generation message");

                    var scopedMessageProcessor = scope
                        .ServiceProvider
                        .GetRequiredService<IEdi835GenerationMessageProcessor>();

                    await scopedMessageProcessor.ProcessMessage(message, cancellationToken);

                    _logger.LogInformation($"Completed processing Edi835Generation message");

                    return true;
                }
                catch (Exception ex) when (IsSkipOpach(ex))
                {
                    return true;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"An error occurred processing Edi835Generation message: {exception.Message}");
                    throw;
                }
            }
        }

        private bool IsSkipOpach(Exception ex)
        {
            return (ex is OpachBankingDetailsMissingException) ||
                (ex is PdfFileNotFoundException pdfEx
                    && string.Equals(pdfEx.Edi835.PaymentTypeCode, PayType.OPACH, StringComparison.OrdinalIgnoreCase)
                    && pdfEx.Message.Contains("Unable to find Archive"));
        }
    }
}
