using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Reminder Fax Service
    /// </summary>
    public interface IReminderFaxService
    {
        /// <summary>
        /// Will process all Reminder Faxes in <see cref="ReminderFaxQueueMessage.ReminderFaxes"/>
        /// /// </summary>
        /// <param name="reminderFaxQueueMessage"></param>
        /// <returns></returns>
        Task ProcessReminderFaxes(ReminderFaxQueueMessage reminderFaxQueueMessage);

        /// <summary>
        /// Will process the given <paramref name="reminderFaxRequest"/>
        /// </summary>
        /// <param name="reminderFaxRequest"></param>
        /// <returns></returns>
        Task ProcessReminderFaxRequest(ReminderFaxRequest reminderFaxRequest);
    }
}
