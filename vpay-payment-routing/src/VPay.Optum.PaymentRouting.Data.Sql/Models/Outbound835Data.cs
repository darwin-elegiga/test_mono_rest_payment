using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPay.Optum.PaymentRouting.Data.Sql.Models
{
    public class Outbound835Data
    {
        public long TransactionId { get; set; }
        public string OutboundRefEO { get; set; }
        public string HeaderData { get; set; }
        public string PaymentData { get; set; }
    }
}
