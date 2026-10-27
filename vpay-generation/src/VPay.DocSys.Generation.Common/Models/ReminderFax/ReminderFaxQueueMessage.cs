using System;

using VPay.Data.Db2.Abstractions.Fax;

namespace VPay.DocSys.Generation.Common
{
    public class ReminderFaxQueueMessage
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public ReminderFax[] ReminderFaxes { get; set; }
    }
}
