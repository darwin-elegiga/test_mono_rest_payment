using System;

using VPay.DocSys.Common;

using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common
{
    public class FaxJobRequest
    {
        public FaxJobRequest(TiffQueueMessage tiffQueueMessage, int pageCount, ulong tiffFileSize, FaxJobSettings faxJobSettings)
        {
            ThrowIf.IsNull(tiffQueueMessage, nameof(tiffQueueMessage));
            ThrowIf.IsNull(faxJobSettings, nameof(faxJobSettings));
            ThrowIf.IsLessThanOrEqualToZero(faxJobSettings.MaxHoursCap, nameof(faxJobSettings.MaxHoursCap));
            if (faxJobSettings.Scale <= 0) { throw new ArgumentException("Must be a positive value", nameof(faxJobSettings.Scale)); }

            CorrespondenceId = tiffQueueMessage.CorrespondenceId;
            Priority = tiffQueueMessage.Priority;
            PhoneNumber = tiffQueueMessage.FaxNumber;
            Tpa = tiffQueueMessage.Tpa;
            BillingEntity = tiffQueueMessage.BillingEntity;
            PayType = tiffQueueMessage.PayType;
            TransactionIds = tiffQueueMessage.TransactionIds.ToArray();
            Origin = tiffQueueMessage.Origin;
            PrintVendor = tiffQueueMessage.PrintVendor;
            ReserveName = GetReserveNameString(tiffQueueMessage.ReserveName);
            SendInstruction = "F";
            CorrespondenceMethod = "Fax";
            BoardUploadDateTime = tiffQueueMessage.BoardUploadTimeStamp;
            PageCount = pageCount;
            FileSize = tiffFileSize;
            AbandonAfterMinutes = CalculateAbandonAfter(ReserveName, tiffFileSize, faxJobSettings);
        }

        public long CorrespondenceId { get; }
        public int Priority { get; }
        public int MaxRetries => 4;
        public string PhoneNumber { get; }
        public string DocumentPath { get; set; }
        public string CorrespondenceMethod { get; }
        public string SendInstruction { get; }
        public string ReserveName { get; }
        public string Tpa { get; }
        public string BillingEntity { get; }
        public string PayType { get; }
        public long[] TransactionIds { get; }
        public string Origin { get; }
        public string PrintVendor { get; }
        public DateTime? BoardUploadDateTime { get; }
        public int PageCount { get; }
        public ulong FileSize { get; }
        public int AbandonAfterMinutes { get; }

        /// <summary>
        /// slowest fax modem rate / bits per byte * seconds per minute
        /// </summary>
        private static double BytesPerMinute => 9600.0 / 8.0 * 60.0;

        private static int CalculateAbandonAfter(string reserveName, ulong fileSize, FaxJobSettings faxJobSettings)
        {
            // business requirement due to real-time sensitivity
            if (reserveName.Equals(FaxReserveName.WARRANTY.ToString())) { return 5; }

            var estimatedFaxTime = fileSize / BytesPerMinute;
            var waitIncrement = 15;

            var minutes = estimatedFaxTime * faxJobSettings.Scale * waitIncrement + // calculate wait time
                waitIncrement +                                                     // add one more increment
                (faxJobSettings.PaddingInSeconds / 60.0);                           // add padding

            // round up to nearest multiple of 5
            // cap using max hours in minutes
            return Math.Min(5 * (int)Math.Ceiling(minutes / 5.0), (faxJobSettings.MaxHoursCap * 60));
        }

        /// <summary>
        /// The Fax System does not have <see cref="FaxReserveName.BFAX"/> and should treat it
        /// the same as <see cref="FaxReserveName.CSS"/> since they are both triggered manually
        /// </summary>
        /// <param name="faxReserveName"></param>
        /// <returns></returns>
        private static string GetReserveNameString(FaxReserveName faxReserveName)
        {
            return (faxReserveName == FaxReserveName.BFAX
                ? FaxReserveName.CSS
                : faxReserveName)
                .ToString();
        }
    }
}
