using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;

namespace VPay.DocSys.Parsing.Common.Inbound835Files
{
    public class RejectionCodes
    {
        #region [-- RejectionCode --]

        public class RejectionCode
        {
            public RejectionCode(string value, string description, Edi835StatusCode edi835StatusCode)
            {
                Value = value;
                Description = description;
                Edi835StatusCode = edi835StatusCode;
            }

            public string Value { get; }
            public string Description { get; }
            public Edi835StatusCode Edi835StatusCode { get; }
        }

        #endregion

        public static readonly RejectionCode EdiError = new RejectionCode("EDI", "Vendor reported an edi data error", Edi835StatusCode.VendorReportedError);
        public static readonly RejectionCode UnknownRemarkCode = new RejectionCode("URC", "Vendor requires updating mapping for an unknown remark code", Edi835StatusCode.UnknownRemarkCode);
        public static readonly RejectionCode ProviderCancelledEnrollment = new RejectionCode("CE", "Vendor reported provider cancelled enrollment", Edi835StatusCode.ProviderCancelledEnrollment);

        private static readonly string unknownDescription = "Vendor reported an unknown response code";
        private static readonly Edi835StatusCode unknownEdi835StatusCode = Edi835StatusCode.UnknownVendorResponse;

        private static readonly Dictionary<string, RejectionCode> mappings = new Dictionary<string, RejectionCode>()
        {
            { EdiError.Value, EdiError },
            { UnknownRemarkCode.Value, UnknownRemarkCode },
            { ProviderCancelledEnrollment.Value, ProviderCancelledEnrollment },
        };

        public static RejectionCode Lookup(string rejectionCode)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(rejectionCode, nameof(rejectionCode));

            if(mappings.TryGetValue(rejectionCode, out var item))
            {
                return item;
            }

            return new RejectionCode(rejectionCode, unknownDescription, unknownEdi835StatusCode);
        }
    }
}
