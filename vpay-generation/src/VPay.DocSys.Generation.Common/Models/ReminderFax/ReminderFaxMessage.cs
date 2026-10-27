
using VPay.Data.Db2.Abstractions.Fax;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Reminder Fax Message
    /// </summary>
    public class ReminderFaxMessage
    {
        public ReminderFax ReminderFax { get; set; }

        public PayorDetail PayorDetail { get; set; }

        public byte[] DistributionPacketBytes { get; set; }

        public string FileName { get; set; }

        public int PageCount { get; set; }
    }
}
