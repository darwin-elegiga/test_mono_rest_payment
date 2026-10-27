CREATE   PROCEDURE [dbo].[usp_InboundBatch_IncompleteBatchDetails]
    @MinutesLag INT = 20 -- Do not alert/report on any batches until they are older than this many minutes
AS
SET NOCOUNT ON;

SELECT      B.InboundBatchId,
            B.OptumBatchId,
            B.Payer835Id,
            B.TotalPaymentCount,
            B.ReceivedPaymentCount,
            B.TotalPaymentAmount,
            B.ReceivedPaymentAmount,
            B.HeaderCount,
            B.RequiredRemittanceAdviceFiles,
            B.ReceivedRemittanceAdviceFiles,
            B.[Status],
            CASE
                WHEN ISSUE_NO_BATCHSUMMARY > 0 THEN 'Batch summary not received'
                WHEN ISSUE_HEADER          > 0 THEN 'Header (EOF) missing for batch'
                WHEN ISSUE_PMT_COUNT       > 0 THEN 'Payment count mismatch'
                WHEN ISSUE_PMT_AMOUNT      > 0 THEN 'Payment amount mismatch'
                WHEN ISSUE_PDF_FILES       > 0 THEN 'RemittanceAdviceFile PDFs missing'
            ELSE 'Unknown reason for incomplete batch' END AS [IncompleteBatchReason],
            B.CreatedDateTime,
            B.LastModifiedDateTime
FROM        vwInboundBatchDetails B
WHERE       (ISSUE_NO_BATCHSUMMARY + ISSUE_PMT_COUNT + ISSUE_PMT_AMOUNT + ISSUE_HEADER  + ISSUE_PDF_FILES) > 0 AND
            B.CreatedDateTime > DateAdd(DAY, -30  , GetDate()) AND
            B.CreatedDateTime < DateAdd(MINUTE, -1 * @MinutesLag, GetDate());