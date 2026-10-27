using System.Collections.Generic;

using VPay.Data.Db2.Abstractions.Fax;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Error Report Builder for <see cref="ReminderFax"/>s
    /// </summary>
    public interface IReminderFaxErrorReportBuilder
    {
        /// <summary>
        /// Builds an HTML error report for Reminder Faxes
        /// </summary>
        /// <param name="reminderFaxErrorDictionary"></param>
        /// <param name="totalReminders"></param>
        /// <returns><see cref="string"/></returns>
        string BuildReport(
            IDictionary<ReminderFaxErrorType, IList<ReminderFaxError>> reminderFaxErrorDictionary, int totalReminders);
    }
}
