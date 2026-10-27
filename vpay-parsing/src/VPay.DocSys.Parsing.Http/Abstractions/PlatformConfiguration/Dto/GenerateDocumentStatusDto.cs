using System;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class GenerateDocumentStatusDto
    {
        public int ConfigurationId { get; set; }
        public string OriginCode { get; set; } = "";
        public string ConfigurationName { get; set; } = "";
        public string ClientCode { get; set; } = "";
        public string BillingEntityCode { get; set; } = "";
        public string PaymentTypeCode { get; set; } = "";
        public string IsGenerateDocumentStatus { get; set; } = "";
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string CreatedByProgram { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime LastModifiedDateTime { get; set; }
        public string LastModifiedByProgram { get; set; } = "";
        public string LastModifiedBy { get; set; } = "";
    }
}
