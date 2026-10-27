using VPay.Queuing;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// This is to abstract the use of the <see cref="PublisherFactory"/> helper method
    /// </summary>
    public interface IQueueService
    {
        /// <summary>
        /// This will publish a <see cref="message"/> using the <see cref="queueConfigurationSettings"/>
        /// </summary>
        /// <param name="queueConfigurationSettings">queue configuration settings</param>
        /// <param name="message">message string to publish</param>
        /// <param name="messagePriority"></param>
        void PublishQueueMessage(
           QueueConfigurationSettings queueConfigurationSettings,
           string message,
           byte? messagePriority = 0);

        /// <summary>
        /// This is used to change the <see cref="QueueConfigurationSettings.QueueName"/> to the <see cref="QueueConfigurationSettings.ErrorQueueName"/>
        /// </summary>
        /// <param name="queueConfigurationSettings"></param>
        /// <returns></returns>
        QueueConfigurationSettings GetErrorQueueConfiguration(QueueConfigurationSettings queueConfigurationSettings);
    }
}
