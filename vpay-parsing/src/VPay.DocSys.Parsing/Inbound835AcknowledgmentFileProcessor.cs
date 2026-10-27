using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client.Exceptions;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;
using VPay.Extensions.Ssh;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// An <see cref="IInbound835FileProcessor"/> that processes <see cref="Inbound835FileType.Acknowledgment"/> files.
    /// </summary>
    public class Inbound835AcknowledgmentFileProcessor : Inbound835FileProcessor, IInbound835AcknowledgmentFileProcessor
    {
        private readonly iSeriesPmbQueueSettings _pmbQueueSettings;
        private readonly IMessagePublisherService _messagePublisherService;
        private readonly AcknowledgmentFileSettings _acknowledgmentFileSettings;
        private readonly ILogger<Inbound835AcknowledgmentFileProcessor> _logger;
        private readonly ISyncPolicy _pmbSendPolicy;

        public Inbound835AcknowledgmentFileProcessor(
            IFileSystem fileSystem,
            ISftpService sftpService,
            IEdi835Repository edi835Repository,
            IInbound835FileParser inbound835FileParser,
            iSeriesPmbQueueSettings pmbQueueSettings,
            IMessagePublisherService messagePublisherService,
            Inbound835FileServiceSettings inbound835FileServiceSettings,
            ILogger<Inbound835AcknowledgmentFileProcessor> logger)
            : base(fileSystem, sftpService, edi835Repository, inbound835FileParser, logger)
        {
            _pmbQueueSettings = pmbQueueSettings;
            _messagePublisherService = messagePublisherService;
            _acknowledgmentFileSettings = inbound835FileServiceSettings.AcknowledgmentFile;
            _logger = logger;

            // create retry policy for sending pmb messages
            if(_acknowledgmentFileSettings.SendAckPmbMessage.Enabled && _acknowledgmentFileSettings.SendAckPmbMessage.Retries > 0)
            {
                _pmbSendPolicy = Policy
                  .Handle<RabbitMQClientException>((e) => !(e is AuthenticationFailureException))
                  .Or<BrokerUnreachableException>((e) => !(e.InnerException is AuthenticationFailureException))
                  .WaitAndRetry(
                      _acknowledgmentFileSettings.SendAckPmbMessage.Retries,
                      (r) => _acknowledgmentFileSettings.SendAckPmbMessage.SleepBetweenRetries,
                      this.OnRetry);
            }
            else
            {
                _pmbSendPolicy = Policy.NoOp();
            }
        }

        public override async Task ProcessFile(Inbound835FileMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message.FilePath, nameof(message), nameof(message.FilePath));

            using(_logger.BeginScope(message, _acknowledgmentFileSettings))
            {
                var ackFile = await ParseAcknowledgmentFile(message.FilePath, cancellationToken);

                // start new log scope with transaction id
                using(_logger.BeginScope(new { ackFile.TransactionId }.ToDictionary()))
                {
                    // get 835 from docsys database
                    var edi835 = await LookupEdi835(ackFile.TransactionId, cancellationToken);

                    // if no 835 is found, it's an error condition
                    if(edi835 == null)
                    {
                        throw _logger.LogErrorAndReturnException($"No 835 request found for transaction: {ackFile.TransactionId}");
                    }

                    // start new log scope with edi835 id
                    using(_logger.BeginScope(new { edi835.Edi835Id }.ToDictionary()))
                    {
                        // log that we found the 835...
                        _logger.LogInformation($"Found 835 request: {edi835.Edi835Id}");

                        // ...process ack file
                        await ProcessAckFile(ackFile, edi835, cancellationToken);
                    }
                }
            }
        }

        private async Task ProcessAckFile(AcknowledgmentFile ackFile, Edi835 edi835, CancellationToken cancellationToken)
        {
            // send ack pmb message
            if(_acknowledgmentFileSettings.SendAckPmbMessage.Enabled)
            {
                await SendAckPmbMessage(ackFile);
            }
            else
            {
                _logger.LogInformation("Send ACKPROC PMB message disabled - skipping message publish.");
            }

            // if ack file has rejection code...
            if(!string.IsNullOrWhiteSpace(ackFile.RejectionCode))
            {
                // get rejection code data
                var rejectionCode = RejectionCodes.Lookup(ackFile.RejectionCode);

                // log warning
                _logger.LogInformation($"{rejectionCode.Description} - transaction: {ackFile.TransactionId}");

                // update 835 status
                await Update835Status(edi835, rejectionCode.Edi835StatusCode, ackFile.FullFileName, ackFile.Contents, cancellationToken);
            }
            else
            {
                // otherwise, update the 835 status to VendorAcknowledged
                await Update835Status(edi835, Edi835StatusCode.VendorAcknowledged, ackFile.FullFileName, ackFile.Contents, cancellationToken);
            }
        }

        private Task SendAckPmbMessage(AcknowledgmentFile ackFile)
        {
            try
            {
                _logger.LogInformation($"Sending ACKPROC PMB message to iSeries: {ackFile.FullFileName}");

                var pmbMessage = new PmbMessage()
                {
                    FromSystem = "PTLACK",
                    ToSystem = "PTLACKR",
                    Family = "PAC",
                    MessageIndex = "000000",
                    OriginAction = "PTLACKP",
                    Payload = ackFile.GetPmbMessagePayload(),
                };

                _pmbSendPolicy.Execute(() =>
                {
                    _messagePublisherService.PublishMessage(_pmbQueueSettings, pmbMessage.ToString());

                }, "Send ACKPROC PMB message to iSeries");

                _logger.LogInformation($"Send ACKPROC PMB message complete.");

                return Task.CompletedTask;
            }
            catch(BrokerUnreachableException exception) when(exception.InnerException is AuthenticationFailureException)
            {
                _logger.LogError(exception.InnerException, $"Sending ACKPROC PMB message failed: {exception.InnerException.Message}");
                throw exception.InnerException;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, $"Sending ACKPROC PMB message failed: {exception.Message}");
                throw;
            }
        }

        private void OnRetry(Exception exception, TimeSpan sleepDuration, int retryCount, Context context)
        {
            // when a BrokerUnreachableException is thrown by rabbit library with an inner exception of AuthenticationFailedException
            // the whole exception stack is too large for graylog, so here we're checking for that scenario and breaking it up
            if(exception is BrokerUnreachableException && exception.InnerException is AuthenticationFailureException)
            {
                exception = exception.InnerException;
            }

            var operation = context.OperationKey ?? "(unknown operation)";

            using(_logger.BeginScope(new Dictionary<string, object>()
            {
                { "RetryCount", retryCount},
                { "SleepDuration", sleepDuration },
                { "Operation", operation },
            }))
            {
                _logger.LogInformation(exception, $"Retry ({retryCount}): {operation}");
            }
        }
    }
}
