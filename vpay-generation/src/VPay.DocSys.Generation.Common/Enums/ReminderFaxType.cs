using System.ComponentModel;

namespace VPay.DocSys.Generation.Common
{
    public enum ReminderFaxType
    {
        /// <summary>
        /// Standard Reminder Fax
        /// </summary>
        [Description("Standard")]
        REMIND_BEG,

        /// <summary>
        /// Final Reminder Fax
        /// </summary>
        [Description("Final")]
        REMIND_EXP
    }
}
