using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{
    public class TransactionOperationRequest
    {
        public IList<long> TransactionIds { get; set; }
    }
}
