using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common
{
    public class TransactionOperationResponse
    {
        public long TransactionId { get; set; }

        public object Result { get; set; }
    }
}
