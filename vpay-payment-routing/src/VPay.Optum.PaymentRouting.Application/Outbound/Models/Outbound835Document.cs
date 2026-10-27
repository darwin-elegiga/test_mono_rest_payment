using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPay.Optum.PaymentRouting.Application.Outbound.Models
{
    public class Outbound835Document
    {
        public Stream Stream { get; set; }

        public string ContentType { get; set; }

        public string FileName { get; set; }
    }
}
