using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrsHelper.Models
{
    internal class RemittanceAdviceFileDto
    {
        public string payer835Id { get; set; } = default!;
        public string inboundZipfile { get; set; } = default!;
        public string userKey { get; set; } = default!;
        public string payerAlias { get; set; } = default!;
        public string originCode { get; set; } = default!;
        public string clientCode { get; set; } = default!;
        public string remitMethod { get; set; } = default!;
        public string pdfExtractPath { get; set; } = default!;
        public string createdBy { get; set; } = default!;
    }
 

}
