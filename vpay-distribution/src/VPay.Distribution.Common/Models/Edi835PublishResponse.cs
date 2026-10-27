using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.Distribution.Common.Models
{
    public class Edi835PublishResponse
    {

        public Edi835PublishResponse()
        {
            Edi835TransactionsFound = new List<long>();
            Edi835TransactionsPublished = new List<long>();
 
            Notes = string.Empty;
        }

        public List<long> Edi835TransactionsFound { get; set; }
        public List<long> Edi835TransactionsPublished { get; set; }
 
 

        public string Notes { get; set; }
    }
}
