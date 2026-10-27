CREATE PROCEDURE [dbo].[usp_InboundBatch_GetByOptumBatchId]
    @OptumBatchId VARCHAR(49) 
AS
BEGIN
SET NOCOUNT ON;

Select      B.Id,
            B.[OptumBatchId],
            B.TotalPaymentCount,
            B.TotalPaymentAmount,
            B.[Status] as [Status]
FROM        [dbo].InboundBatch as B
WHERE       B.[OptumBatchId]=@OptumBatchId;
END