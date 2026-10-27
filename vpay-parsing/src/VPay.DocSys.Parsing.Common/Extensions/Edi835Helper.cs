using System;
using System.Collections.Generic;
using System.Text;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common
{
    public static class Edi835Helper
    {
        //public static Edi835Metadata ExtractEdi835Metadata(this Edi835CreationMessage message)
        //{
        //    return new Edi835Metadata
        //    {
        //        TransactionId=message.TransactionId,
        //        TaxID = message.TaxIdentificationNumber,
        //        OriginCode=message.OriginCode,
        //        ClientCode=message.ClientCode,
        //        BillingEntityCode=message.BillingEntityCode,
        //        Amount=message.Amount,
        //        IsReissue = message.CheckStatus=="RI",
        //        ClaimDate= ConvertToNullableDateTime(message.ClaimDate),
        //        LoadDate= ConvertToNullableDateTime(message.LoadDate),
        //        NonFundedPaymentDetailId = message.NonFundedPaymentDetailId,
        //        ClaimId = message.ClaimId,
        //        BaseFileName = message.GetBaseFilename(),
        //        PaymentId=message.UserKey,
        //        PayeeName = message.PayeeName,
        //        PaymentTypeCode = message.PaymentTypeCode,
        //        StatusFile = System.IO.Path.GetFileName( message.StatusFilePath)
        //    };

 
        //}

        private static DateTime? ConvertToNullableDateTime(string textDate)
        {
            try {
                return (DateTime?)DateTime.ParseExact(textDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch {} // Move on to next method
           
            if ( DateTime.TryParse(textDate, out DateTime parsedDateTime))
            {
                return (DateTime?)parsedDateTime;
            }
            return (DateTime?)null;
        }
    }
}
