using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;

namespace VPay.Distribution
{
    public class MailDistributionQueueMessageProcessor : IMailDistributionQueueMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly IMailService _mailService;

        public MailDistributionQueueMessageProcessor(
            IMailService mailService,
            ILogger<MailDistributionQueueMessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(mailService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _mailService = mailService;
            _logger = logger;
        }

        public Task ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default)
        {
            using (_logger.CreateLogScope(message))
            {
                return _mailService.DistributeMail(message, cancellationToken);
            }
        }
    }
}
