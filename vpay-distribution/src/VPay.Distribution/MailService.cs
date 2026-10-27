using Microsoft.Extensions.Logging;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;

namespace VPay.Distribution
{
    public class MailService : IMailService
    {
        private readonly IMailFileProcessor _mailFileProcessor;
        private readonly IEventMessagePublisher _eventMessagePublisher;
        private readonly IEdi835MessagePublisher _edi835MessagePublisher;
        private readonly ILogger<MailService> _logger;

        //Added as a static function so unit tests can override the detail so they run faster.
        public static Func<int, Task> Delay = (delayInMs) => Task.Delay(delayInMs);

        public MailService(
            IMailFileProcessor mailFileProcessor,
            IEventMessagePublisher eventMessagePublisher,
            IEdi835MessagePublisher edi835MessagePublisher,
            ILogger<MailService> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(mailFileProcessor, nameof(mailFileProcessor));
            ArgumentHelpers.ThrowIf.IsNull(eventMessagePublisher, nameof(eventMessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(edi835MessagePublisher, nameof(edi835MessagePublisher));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _mailFileProcessor = mailFileProcessor;
            _eventMessagePublisher = eventMessagePublisher;
            _edi835MessagePublisher = edi835MessagePublisher;
            _logger = logger;
        }

        public async Task DistributeMail(DocDistMsg message, CancellationToken cancellationToken)
        {
            ArgumentHelpers.ThrowIf.IsNull(message, nameof(message));

            using (_logger.CreateLogScope(message))
            {
                if (!message.Transactions.Any())
                {
                    _logger.LogInformation("No mail to distribute");
                    return;
                }

                try
                {
                    var result = await _mailFileProcessor.Process(message);

                    _logger.LogInformation("Distributed {TransactionCount} mail payments", result.Count);
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
}
