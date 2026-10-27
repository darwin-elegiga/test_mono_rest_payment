using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// Provides methods for processing inbound 835 files.
    /// </summary>
    public class Inbound835FileService : IInbound835FileService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEdi835Repository _edi835Repository;
        private readonly IInbound835FileParser _inbound835FileParser;
        private readonly Inbound835FileQueueSettings _queueSettings;
        private readonly Inbound835FileServiceSettings _serviceSettings;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly IInbound835PaymentPostedFileProcessor _paymentPostedFileProcessor;
        private readonly IInbound835AcknowledgmentFileProcessor _acknowledgmentFileProcessor;
        private readonly IInbound835RemittanceAdviceFileProcessor _remittanceAdviceFileProcessor;
        private readonly ILogger<Inbound835FileService> _logger;

        public Inbound835FileService(
            IFileSystem fileSystem,
            IEdi835Repository edi835Repository,
            IInbound835FileParser inbound835FileParser,
            Inbound835FileQueueSettings queueSettings,
            Inbound835FileServiceSettings serviceSettings,
            IMessagePublisherService messagePublisherService,
            IInbound835PaymentPostedFileProcessor paymentPostedFileProcessor,
            IInbound835AcknowledgmentFileProcessor acknowledgmentFileProcessor,
            IInbound835RemittanceAdviceFileProcessor remittanceAdviceFileProcessor,
            ILogger<Inbound835FileService> logger
            )
        {
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(edi835Repository, nameof(edi835Repository));
            ArgumentHelpers.ThrowIf.IsNull(inbound835FileParser, nameof(inbound835FileParser));
            ArgumentHelpers.ThrowIf.IsNull(queueSettings, nameof(queueSettings));
            ArgumentHelpers.ThrowIf.IsNull(serviceSettings, nameof(serviceSettings));
            ArgumentHelpers.ThrowIf.IsNull(messagePublisherService, nameof(messagePublisherService));
            ArgumentHelpers.ThrowIf.IsNull(paymentPostedFileProcessor, nameof(paymentPostedFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(acknowledgmentFileProcessor, nameof(acknowledgmentFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(remittanceAdviceFileProcessor, nameof(remittanceAdviceFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _fileSystem = fileSystem;
            _edi835Repository = edi835Repository;
            _inbound835FileParser = inbound835FileParser;
            _queueSettings = queueSettings;
            _serviceSettings = serviceSettings;
            _messagePublisherService = messagePublisherService;
            _paymentPostedFileProcessor = paymentPostedFileProcessor;
            _acknowledgmentFileProcessor = acknowledgmentFileProcessor;
            _remittanceAdviceFileProcessor = remittanceAdviceFileProcessor;
            _logger = logger;
        }

        public Task QueueFileForProcessing(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(filePath, nameof(filePath));

            var message = new Inbound835FileMessage(filePath);

            return QueueFileForProcessing(message, cancellationToken);
        }

        public Task QueueFileForProcessing(Inbound835FileMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            using(_logger.BeginScope(message, _serviceSettings))
            {
                try
                {
                    _logger.LogInformation($"Begin queuing inbound 835 file: {message.FilePath.Replace(Environment.NewLine, "")}");

                    _messagePublisherService.PublishMessage(_queueSettings, JsonConvert.SerializeObject(message));

                    _logger.LogInformation($"Queuing inbound 835 file complete.");
                }
                catch(Exception exception)
                {
                    _logger.LogError(exception, $"Error queuing inbound 835 file: {message.FilePath.Replace(Environment.NewLine, "")}");
                    throw;
                }
            }

            return Task.CompletedTask;
        }

        public Task ProcessFile(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(filePath, nameof(filePath));

            var message = new Inbound835FileMessage(filePath);

            return this.ProcessFile(message, cancellationToken);
        }

        public async Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message.FilePath, nameof(message), nameof(message.FilePath));

            using(_logger.BeginScope(message, _serviceSettings))
            {
                try
                {
                    _logger.LogInformation($"Processing inbound 835 file: {message.FilePath.Replace(Environment.NewLine, "")}");

                    if(!_fileSystem.File.Exists(message.FilePath))
                    {
                        throw new FileNotFoundException("File not found.", message.FilePath);
                    }

                    switch(_inbound835FileParser.GetFileType(message.FilePath))
                    {
                        case Inbound835FileType.Acknowledgment:
                            await _acknowledgmentFileProcessor.ProcessFile(message, cancellationToken);
                            break;

                        case Inbound835FileType.PaymentPosted:
                            await _paymentPostedFileProcessor.ProcessFile(message, cancellationToken);
                            break;

                        case Inbound835FileType.RemittanceAdvice:
                        case Inbound835FileType.BlueRemittanceNotification:
                            await _remittanceAdviceFileProcessor.ProcessFile(message, cancellationToken);
                            break;

                        default:
                            throw new NotSupportedException($"Invalid or unsupported inbound 835 file type: {message.FilePath}");
                    }

                    _logger.LogInformation($"Processing inbound 835 file completed.");
                }
                catch(Exception exception)
                {
                    _logger.LogError(exception, $"Processing inbound 835 file failed: {exception.Message}");
                    throw;
                }
            }
        }

        public Task Send835ToAvaility(long transactionId, CancellationToken cancellationToken = default)
        {
            return _remittanceAdviceFileProcessor.Send835ToAvaility(transactionId, cancellationToken);
        }

        public Task SendBrnToAvaility(long transactionId, CancellationToken cancellationToken = default)
        {
            return _remittanceAdviceFileProcessor.SendBrnToAvaility(transactionId, cancellationToken);
        }
    }
}
