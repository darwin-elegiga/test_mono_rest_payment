using Microsoft.Extensions.Logging;


using System.Threading;
using System.Threading.Tasks;

using VPay.Distribution.Common;
using VPay.Distribution.Common.Helpers;
using VPay.DocSys.Common;

namespace VPay.Distribution
{
    public class CustomSendBackDistributionQueueMessageProcessor : ICustomSendBackDistributionQueueMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly ICustomSendBackService _customSendBackService;

        public CustomSendBackDistributionQueueMessageProcessor(
            ICustomSendBackService customSendBackService,
            ILogger<CustomSendBackDistributionQueueMessageProcessor> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(customSendBackService, nameof(customSendBackService));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _customSendBackService = customSendBackService;
            _logger = logger;
        }

        public Task ProcessMessage(DocDistMsg message, CancellationToken cancellationToken = default)
        {
            using (_logger.CreateLogScope(message))
            {
                return _customSendBackService.DistributeCustomSendBacks(message, cancellationToken);
            }
        }
    }
}
