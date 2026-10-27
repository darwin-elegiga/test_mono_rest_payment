using VPay.Optum.PaymentRouting.Data.Abstractions;

namespace VPay.Optum.PaymentRouting.Data.Sql.BatchSummary
{
    public class BatchSummary
    {
        public long InboundBatchId { get; set; }
        public string OptumBatchId { get; set; }
        public int? HeaderCount { get; set; }
        public int? TotalPaymentCount { get; set; }
        public int? ActualPaymentCount { get; set; }
        public decimal? TotalPaymentAmount { get; set; }
        public decimal? ActualPaymentAmount { get; set; }
        public BatchStatus Status { get; set; }

        public int? RequiredPdfs { get; set; }
        public int? FoundPdfs { get; set; }

        public bool IsBatchReceived()
        {
            return (
                // OptumBatchId must not be empty/null/whitespace only
                (!string.IsNullOrWhiteSpace(OptumBatchId)) && 
                // Must have EXACTLY 1 InboundHeader record
                (HeaderCount ?? 0) == 1 && 
                // TotalPaymentCount must not be null (meaning it was received on the InboundBatch record)
                // AND must equal ActualPaymentCount (Count of InboundPayment records)
                (TotalPaymentCount ?? -1) == (ActualPaymentCount ?? 0) &&

                // TotalPaymentAmount must not be null (meaning it was received on the InboundBatch record)
                // AND must equal ActualPaymentAmount (Sum of InboundPayment.PaymentAmount values)
                (TotalPaymentAmount ?? -1m) == (ActualPaymentAmount ?? 0m) &&

                // If Pdfs are required, make sure all are present (there's a 1:1 on matching, so this math makes sense)
                (
                    (RequiredPdfs.HasValue == false && FoundPdfs.HasValue == false) ||

                    (RequiredPdfs.HasValue && FoundPdfs.HasValue && RequiredPdfs.Value == FoundPdfs.Value)
                )
            );
        }


        /// <summary>
        /// A batch is valid so long as we have NOT received:
        /// - The value received from Optum for the OptumBatchId is not empty/null/whitespace.
        /// - More than 1 InboundHeader
        /// And once the InboundBatch record has been received (TotalPaymentCount AND TotalPaymentAmount) are not null
        /// - Count of InboundPayment records CANNOT be greater thanTotalPaymentCount
        /// - Sum of InboundPayment.PaymentAmount values CANNOT be greater than TotalPaymentAmount
        /// </summary>
        /// <returns></returns>
        public bool IsBatchValid()
        {
            return (
                (!string.IsNullOrWhiteSpace(OptumBatchId)) && // Optum's OptumBatchId must not be empty/null/whitespace only
                (HeaderCount ?? 0) < 2 &&
                (!TotalPaymentCount.HasValue || TotalPaymentCount.Value >= (ActualPaymentCount ?? 0)) &&
                (!TotalPaymentAmount.HasValue || TotalPaymentAmount.Value >= (ActualPaymentAmount ?? 0m))
            );
        }


    }
}
