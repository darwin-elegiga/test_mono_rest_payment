using System;

namespace VPay.DocSys.Parsing.Common
{
    /// <summary>
    /// Since comparing Legacy835 to the rewrite is only "burn-in" code,
    /// adding this as a DTO to avoid polluting the VPay.DocSys.Common nuget package.
    /// This <i>should</i> mirror the object VPay.DocSys.Generation.Edi835.Data.Abstractions.Models.LegacyActivityResult
    /// </summary>
    public class LegacyActivityResultDTO
    {
        public string OriginCode { get; set; }
        public string ClientCode { get; set; }
        public string StsFileName { get; set; }
        public string TgzFileName { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime TgzTime { get; set; }

    }
}
