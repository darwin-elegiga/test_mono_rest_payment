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
    public class FaxDistributionQueueMessageProcessor : IFaxDistributionQueueMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly IFaxService _faxService;

        public FaxDistributionQueueMessageProcessor(
            ILogger<FaxDistributionQueueMessageProcessor> logger,
            IFaxService faxService)
        {
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));
            ArgumentHelpers.ThrowIf.IsNull(faxService, nameof(faxService));

            _logger = logger;
            _faxService = faxService;
        }

        public Task ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default)
        {
            using (_logger.CreateLogScope(message))
            {
                return _faxService.DistributeFaxes(message, cancellationToken);
            }
        }
    }
}
