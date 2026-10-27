CREATE PROCEDURE [dbo].[usp_InboundBatch_Summary]
    @InboundBatchId BIGINT 
AS
BEGIN
SET NOCOUNT ON;

SELECT      B.Id as InboundBatchId,
            B.[OptumBatchId],
            MIN( IsNull(E.Id,0)/IsNull(E.Id,1) ) as HeaderCount,
            Sum( Case WHEN IsNull(P.RemitMethod, 'DATA') = 'DATA' THEN 0 ELSE 1 END ) As RequiredPdfs,
            Sum( Case WHEN IsNull(R.RemitMethod, 'DATA') = 'DATA' THEN 0 ELSE 1 END ) As FoundPdfs,
            B.TotalPaymentCount,
            Count(IsNull(P.Id,0)) as ActualPaymentCount,
            B.TotalPaymentAmount,
            Sum(P.PaymentAmount) as ActualPaymentAmount,
            B.[Status] as [Status]
FROM        [dbo].InboundBatch as B
            LEFT JOIN [dbo].InboundPayment as P
                ON B.Id=P.InboundBatchId
            LEFT JOIN [dbo].InboundHeader as E
                ON B.Id=E.InboundBatchId
            LEFT JOIN dbo.vwRemittanceAdviceFile R
                ON P.Payer835Id=R.Payer835Id AND P.UserKey=R.UserKey
WHERE       B.Id=@InboundBatchId
GROUP BY    B.Id, B.[OptumBatchId], B.TotalPaymentCount, B.TotalPaymentAmount, B.[Status];
 
END