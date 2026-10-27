using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;

namespace VPay.Distribution
{
    public class FaxService : IFaxService
    {
        private readonly IFaxFileProcessor _faxFileProcessor;
        private readonly IRedCardFaxFileProcessor _redCardFaxFileProcessor;
        private readonly IFaxMessagePublisher _faxMessagePublisher;
        private readonly IEventMessagePublisher _eventMessagePublisher;
        private readonly IEdi835MessagePublisher _edi835MessagePublisher;
        private readonly ILogger<FaxService> _logger;

        //Added as a static function so unit tests can override the detail so they run faster.
        public static Func<int, Task> Delay = (delayInMs) => Task.Delay(delayInMs);

        public FaxService(
            IFaxFileProcessor faxFileProcessor,
            IRedCardFaxFileProcessor redCardFaxFileProcessor,
            IFaxMessagePublisher faxMessagePublisher,
            IEventMessagePublisher eventMessagePublisher,
            IEdi835MessagePublisher edi835MessagePublisher,
            ILogger<FaxService> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(faxFileProcessor, nameof(faxFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(redCardFaxFileProcessor, nameof(redCardFaxFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(faxMessagePublisher, nameof(faxMessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(eventMessagePublisher, nameof(eventMessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(edi835MessagePublisher, nameof(edi835MessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _faxFileProcessor = faxFileProcessor;
            _redCardFaxFileProcessor = redCardFaxFileProcessor;
            _faxMessagePublisher = faxMessagePublisher;
            _eventMessagePublisher = eventMessagePublisher;
            _edi835MessagePublisher = edi835MessagePublisher;
            _logger = logger;
        }

        public async Task DistributeFaxes(DocDistMsg message, CancellationToken cancellationToken)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            using (_logger.CreateLogScope(message))
            {
                if (!message.Transactions.Any())
                {
                    _logger.LogInformation("No faxes to distribute");
                    return;
                }

                // if there are all correspondence ids, publish messages directly to the tiff queue
                var canPublishMessages = message.Transactions.TrueForAll(_ => _.CorrespondenceId.HasValue);

                // .ctrl files are only created if you cannot publish messages
                var createControlFile = !canPublishMessages;

                //TODO: remove legacy file generation at some point
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    { "CreateControlFile", createControlFile},
                }))
                {
                    try
                    {
                        if (canPublishMessages)
                        {
                            _faxMessagePublisher.PublishToTiffQueue(
                                message.Origin,
                                message.TPA,
                                message.BillingEntity,
                                message.BoardUploadTimeStamp,
                                message.Transactions.ToArray());

                            _logger.LogInformation("Published {Count} fax transactions", message.BatchSize);
                        }

                        await RunLegacyFaxFileCreation(message, createControlFile);
                    }
                    finally
                    {
                        await Delay(5000);
                        _eventMessagePublisher.PublishEvents(message);
                        _ = await _edi835MessagePublisher.PublishEdi835Messages(message, cancellationToken);
                    }
                }
            }
        }

        private async Task RunLegacyFaxFileCreation(DocDistMsg message, bool createControlFile)
        {
            DistributionProcessResult result;

            // TODO: remove legacy file generation at some point,
            // but keep the db updates (if any)
            // currently, a message will be logged when a control file is created
            // always call legacy process to create only the .fax file (for production support)
            // and update the database
            try
            {
                if (message.IsRedCard)
                {
                    result = await _redCardFaxFileProcessor.Process(message, createControlFile);
                }
                else
                {
                    result = await _faxFileProcessor.Process(message, createControlFile);
                }
            }
            catch (Exception exception)
            {
                if (createControlFile)
                {
                    throw;
                }
                else
                {
                    _logger.LogWarning(exception, "Failed to write legacy .fax file.");
                }
                return;
            }

            if (createControlFile)
            {
                _logger.LogInformation("Processed {Count} legacy fax transactions to {FileName}", result.Count, result.FileName);
            }
            else
            {
                _logger.LogInformation("Created {FileName} for {Count} legacy fax transactions", result.FileName, result.Count);
            }
        }
    }
}
