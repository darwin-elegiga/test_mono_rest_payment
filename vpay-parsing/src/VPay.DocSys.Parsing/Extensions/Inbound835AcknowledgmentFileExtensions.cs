using System;
using System.Collections.Generic;
using System.Text;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;

namespace VPay.DocSys.Parsing
{
    public static class Inbound835AcknowledgmentFileExtensions
    {
        /// <summary>
        /// Constructs the payload for a ACKPROC PMB message.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the payload for and ACKPROC PMB message.</returns>
        public static string GetPmbMessagePayload(this AcknowledgmentFile ackFile)
        {
            ArgumentHelpers.ThrowIf.IsNull(ackFile, nameof(ackFile));

            return new StringBuilder(450)
                .Append("ACKPROC".PadRight(10)).Append("|")
                .Append((ackFile.PaymentId ?? "").PadRight(100)).Append("|")
                .Append(ackFile.TransactionId.ToString("D20")).Append("|")
                .Append((ackFile.AckTimeStamp ?? "").PadRight(26)).Append("|")
                .Append((ackFile.TypeCode ?? "").PadRight(20)).Append("|")
                .Append((ackFile.FileName ?? "").PadRight(255)).Append("|")
                .Append((ackFile.RejectionCode ?? "").PadRight(10)).Append("|")
                .ToString();
        }
    }
}
