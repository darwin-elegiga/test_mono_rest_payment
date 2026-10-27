using System;

namespace VPay.DocSys.Parsing.Common
{
    public class RedCardParsingMessage
    {
        public RedCardParsingMessage()
        {
            this.MessageCreated = this.NextStatusCheck = DateTime.Now;
        }

        public string FilePath { get; set; }

        public DateTime MessageCreated { get; set; }

        public DateTime NextStatusCheck { get; set; }

        public int DelayCount { get; set; }
    }
}
