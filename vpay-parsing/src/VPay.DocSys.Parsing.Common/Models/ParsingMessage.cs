using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{
    public class ParsingMessage
    {
        public ParsingMessage()
        {
            MessageCreated = DateTime.Now;
        }
        public string StatusFilePath { get; set; }
        public IList<long> TransactionIds { get; set; }
        public bool IsRegenRequest { get; set; }

        public DateTime MessageCreated { get; }
    }
}
