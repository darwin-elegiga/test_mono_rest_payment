using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrsHelper.Models
{
    /// <summary>
    /// This is all the information needed to "pretend" a batch is coming
    /// in from Optum PRS.  
    /// </summary>
    public class PrsBatchDto
    {
        public PrsBatchDto(string batchId, string payerAlias, string payer835Id, string clientCode, string originCode, BatchRecordDto batchRecord, List<PaymentRecordDto> paymentRecords)
        {
            BatchId = batchId;
            PayerAlias = payerAlias;
            Payer835Id = payer835Id;
            ClientCode = clientCode;
            OriginCode = originCode;
            BatchRecord = batchRecord;
            PaymentRecords = paymentRecords;
        }

        public string BatchId { get; set; }= default!;
        public string PayerAlias { get; set; } = default!;
        public string Payer835Id { get; set; } = default!;
        public string ClientCode { get; set; } = default!;
        public string OriginCode { get; set; } = default!;

        public BatchRecordDto BatchRecord { get; set; } = default!;
        public List<PaymentRecordDto> PaymentRecords { get; set; } = default!;
    }
}
