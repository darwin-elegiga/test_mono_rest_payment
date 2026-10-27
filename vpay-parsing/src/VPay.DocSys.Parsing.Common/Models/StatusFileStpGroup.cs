using System.Collections.Generic;

namespace VPay.DocSys.Parsing.Common
{
    public class StatusFileStpGroup
    {
        public string NetworkName { get; set; }
        public string EnrollType { get; set; }
        public string Edi835Type { get; set; }
        public IEnumerable<long> TransactionIds { get; set; } = new List<long>();
    }
}
