using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Reminder Fax Message Processor
    /// </summary>
    public interface IReminderFaxMessageProcessor
    {
        /// <summary>
        /// Process the <see cref="ReminderFaxMessage"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ProcessMessage(ReminderFaxMessage reminderFaxMessage);
    }
}
