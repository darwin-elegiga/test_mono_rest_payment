using System;
using System.Xml.Serialization;
using VPay.Data.Db2.Abstractions.Fax;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common
{
    public class CoverSheetParameters
    {
        public CoverSheetParameters()
        {
            CurrentDate = DateTime.Now;
        }

        public CoverSheetParameters(ReminderFax reminderFax, PayorDetail payorDetail, string vpayFaxNumber)
            : this()
        {
            ThrowIf.IsNull(reminderFax, nameof(reminderFax));
            ThrowIf.IsNull(payorDetail, nameof(payorDetail));
            ThrowIf.IsNullOrEmpty(vpayFaxNumber, nameof(vpayFaxNumber));

            TransactionId = reminderFax.SeTransactionId;
            ProviderName = reminderFax.PayeeName;
            FaxNumber = reminderFax.UseFaxNumber;
            OriginalFaxDate = reminderFax.LastFaxDate;
            Balance = reminderFax.AvailableBalance;
            TpaCode = reminderFax.TPACode;

            TpaName = payorDetail.PayorName;
            TpaPhone = payorDetail.PayorPhone;
            VPayPhone = payorDetail.VPayPhone;

            VPayFax = vpayFaxNumber;

            IsFinal = ReminderFaxType.REMIND_EXP
                .ToString()
                .Equals(reminderFax.ReminderType, StringComparison.InvariantCultureIgnoreCase);
        }

        public long TransactionId { get; set; }
        public bool IsFinal { get; set; }

        public string TpaCode { get; set; }
        public string TpaName { get; set; }
        public string TpaPhone { get; set; }

        public string ProviderName { get; set; }
        public string FaxNumber { get; set; }

        public string VPayPhone { get; set; }
        public string VPayFax { get; set; }

        [XmlElement(DataType = "date")]
        public DateTime CurrentDate { get; set; }
        [XmlElement(DataType = "date")]
        public DateTime OriginalFaxDate { get; set; }

        public decimal Balance { get; set; }
        public int PageCount { get; set; }
    }
}
