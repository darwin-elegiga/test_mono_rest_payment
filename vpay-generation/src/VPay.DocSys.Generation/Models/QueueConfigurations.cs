using VPay.Queuing;

namespace VPay.DocSys.Generation.Models
{
    /// <summary>
    /// This contains all the <see cref="QueueConfigurationSettings"/> for the project
    /// </summary>
    public class QueueConfigurations
    {
        public QueueConfigurationSettings BundledFaxQueue { get; set; }
        public QueueConfigurationSettings FaxMessageQueue { get; set; }
        public QueueConfigurationSettings CorrespondenceQueue { get; set; }
        public QueueConfigurationSettings TiffQueue { get; set; }
        public QueueConfigurationSettings FaxJobDataQueue { get; set; }
        public QueueConfigurationSettings FaxRetryQueue { get; set; }
        public QueueConfigurationSettings ReminderFaxQueue { get; set; }
        public QueueConfigurationSettings InventoryDriverRecordQueue { get; set; }
        public QueueConfigurationSettings Edi835GenerationQueue { get; set; }
        public DocumentGenerationQueueConfigurationSettings DocumentGenerationQueue { get; set; }
        public QueueConfigurationSettings Edi835LegacyCompareTransactionQueue { get; set; }
    }
}
