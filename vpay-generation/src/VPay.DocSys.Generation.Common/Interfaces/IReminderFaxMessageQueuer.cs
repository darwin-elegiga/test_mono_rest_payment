using System;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions.Fax;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Reminder Fax Message Queuer
    /// </summary>
    public interface IReminderFaxMessageQueuer
    {
        /// <summary>
        /// Queues the <see cref="ReminderFax"/>s
        /// after calling DB2 to fetch them with the given parameters
        /// </summary>
        /// <param name="message"></param>
        Task QueueReminderFaxes(ClientType clientType, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Queues the <see cref="ReminderFaxMessage"/>
        /// </summary>
        /// <param name="reminderFaxMessage"></param>
        /// <returns></returns>
        void QueueReminderFaxMessage(ReminderFaxMessage reminderFaxMessage);
    }
}
