using System;
using System.Collections.Generic;
using System.Text;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    public class StatusFileParsingQueueSettings : QueueConfigurationSettings
    {
    }

    public class OutboundRedCardDataQueueSettings : QueueConfigurationSettings
    {
    }

    public class InventoryDriverFileQueueSettings : QueueConfigurationSettings
    {
    }

    public class InventoryDriverRecordQueueSettings : QueueConfigurationSettings
    {
    }

    public class DocumentTransactionQueueSettings : QueueConfigurationSettings, IConsumerTimeoutQueueConfiguration
    {
        public long ConsumerTimeout { get; set; }
    }

    public class DocumentDistributionQueueSettings : QueueConfigurationSettings
    {
    }

    public class RedCardDistributionQueueSettings : QueueConfigurationSettings
    {
    }

    public class Inbound835FileQueueSettings : QueueConfigurationSettings
    {
    }

    public class iSeriesPmbQueueSettings : QueueConfigurationSettings
    {
    }

    public class Edi835GenerationQueueSettings : QueueConfigurationSettings
    {
    }

    public static class QueueConfigurationSettingsExtensions
    {
        public static void Validate(this QueueConfigurationSettings settings, string queueConfigurationName = null)
        {
            var prefix = string.IsNullOrEmpty(queueConfigurationName) ? "" : queueConfigurationName + ".";
            DocSys.Common.ArgumentHelpers.ThrowIf.IsNull(settings.Host, prefix + nameof(settings.Host));
            DocSys.Common.ArgumentHelpers.ThrowIf.IsNull(settings.QueueName, prefix + nameof(settings.QueueName));
            DocSys.Common.ArgumentHelpers.ThrowIf.IsNull(settings.UserName, prefix + nameof(settings.UserName));
            DocSys.Common.ArgumentHelpers.ThrowIf.IsNull(settings.Password, prefix + nameof(settings.Password));
        }
    }
}
