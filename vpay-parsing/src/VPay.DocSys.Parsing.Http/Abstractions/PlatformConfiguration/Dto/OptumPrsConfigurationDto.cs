using System;

namespace VPay.DocSys.Parsing.Http.Abstractions.PlatformConfiguration.Dto
{
    public class OptumPrsConfigurationDto
    {
        public long ConfigurationId { get; set; }

        public string ConfigurationName { get; set; } = default!;

        public string OriginCode { get; set; } = default!;

        public string ClientCode { get; set; } = default!;

        public string BillingEntityCode { get; set; } = default!;

        public bool IsPrsEnabled { get; set; }

        public bool IsVPayAlwaysDecision { get; set; }

        public string PrfClientGroup { get; set; } = default!;

        public DateTime BeginDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public string CreatedByProgram { get; set; } = default!;

        public string CreatedBy { get; set; } = default!;

        public DateTime LastModifiedDateTime { get; set; }

        public string LastModifiedByProgram { get; set; } = default!;

        public string LastModifiedBy { get; set; } = default!;
    }
}
