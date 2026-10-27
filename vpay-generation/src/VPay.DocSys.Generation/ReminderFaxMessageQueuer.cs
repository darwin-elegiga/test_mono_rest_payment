using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Linq;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions;
using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using VPay.Queuing;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class ReminderFaxMessageQueuer : IReminderFaxMessageQueuer
    {
        private readonly QueueConfigurationSettings _reminderFaxQueueConfigurationSettings;
        private readonly IFax _faxRepository;
        private readonly IQueueService _queueService;
        private readonly ILogger<ReminderFaxMessageQueuer> _logger;

        public ReminderFaxMessageQueuer(
            QueueConfigurations queueConfigurations,
            IFax faxRepository,
            IQueueService queueService,
            ILogger<IMessagePublisher> loggerMessagePublisher,
            ILogger<ReminderFaxMessageQueuer> logger)
        {
            ThrowIf.IsChildPropertyNull(queueConfigurations.ReminderFaxQueue, nameof(queueConfigurations), nameof(queueConfigurations.ReminderFaxQueue));
            ThrowIf.IsNull(faxRepository, nameof(faxRepository));
            ThrowIf.IsNull(queueService, nameof(queueService));
            ThrowIf.IsNull(loggerMessagePublisher, nameof(loggerMessagePublisher));
            ThrowIf.IsNull(logger, nameof(logger));

            _reminderFaxQueueConfigurationSettings = queueConfigurations.ReminderFaxQueue;
            _faxRepository = faxRepository;
            _queueService = queueService;
            _logger = logger;
        }

        public async Task QueueReminderFaxes(ClientType clientType, DateTime startDate, DateTime endDate)
        {
            var reminderFaxes = await _faxRepository.GetReminderFaxes(clientType, startDate, endDate);
            var count = reminderFaxes.Count();

            using (_logger.BeginScope(new
            {
                StartDate = startDate,
                EndDate = endDate,
                ClientType = clientType.ToString(),
                Count = count
            }))
            {
                if (count == 0)
                {
                    _logger.LogInformation("No Reminder Faxes found for {ClientType} from {StartDate} to {EndDate}",
                        clientType.ToString().Replace(Environment.NewLine, ""),
                        startDate,
                        endDate);
                }
                else
                {
                    var groupFaxes = reminderFaxes.GroupBy(_ => _.TPACode);

                    foreach (var faxes in groupFaxes)
                    {
                        var messageString = JsonConvert.SerializeObject(new MessageWrapper<ReminderFaxQueueMessage>(new ReminderFaxQueueMessage
                        {
                            EndDate = endDate,
                            StartDate = startDate,
                            ReminderFaxes = faxes.ToArray()
                        }));

                        _queueService.PublishQueueMessage(_reminderFaxQueueConfigurationSettings, messageString);

                        _logger.LogInformation("Published {MessageCount} Messages for {TPA} Reminder Faxes to Queue",
                            faxes.Count(), faxes.Key);
                    }

                    _logger.LogInformation("Published Message for {Count} {ClientType} Reminder Faxes to Queue",
                        count, clientType.ToString());
                }
            }
        }

        public void QueueReminderFaxMessage(ReminderFaxMessage reminderFaxMessage)
        {
            ThrowIf.IsNull(reminderFaxMessage, nameof(reminderFaxMessage));

            var messageString = JsonConvert.SerializeObject(new MessageWrapper<ReminderFaxMessage>(reminderFaxMessage));

            _queueService.PublishQueueMessage(_reminderFaxQueueConfigurationSettings, messageString);
            _logger.LogInformation("Published Reminder Fax Message");
        }
    }
}
