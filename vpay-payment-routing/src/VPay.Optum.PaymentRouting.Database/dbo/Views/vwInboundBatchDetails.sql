Create VIEW dbo.vwInboundBatchDetails AS
WITH 
cteBatchesOfInterest AS
(
    SELECT      B.Id AS InboundBatchId,
                B.OptumBatchId,
                B.Payer835Id,
                IsNull(B.TotalPaymentCount,0) AS TotalPaymentCount, 
                IsNull(B.TotalPaymentAmount,0.0) as TotalPaymentAmount, -- 0.00 dollar amount is a valid value...
                Cast(CASE WHEN B.TotalPaymentAmount IS NULL THEN 1 ELSE 0 END AS INT) AS TotalPaymentAmountIsNull,
                B.[Status],
                B.CreatedDateTime,
                B.LastModifiedDateTime 
    FROM        dbo.InboundBatch B
    WHERE       B.[Status]  NOT IN ( 'BatchComplete', 'BatchCancelled ') 
)
, cteHeaderCount AS (
    SELECT      BoI.InboundBatchId, CASE WHEN H.Id Is NULL THEN 0 ELSE 1 END as HeaderCount
    FROM        cteBatchesOfInterest as BoI
                LEFT JOIN dbo.InboundHeader AS H
                    ON BoI.InboundBatchId= H.InboundBatchId
)
, ctePaymentCount AS (
    SELECT      BoI.InboundBatchId, 
                SUM(CASE WHEN P.Id Is NULL THEN 0 ELSE 1 END) as PaymentCount,
                CAST(Sum(IsNull(P.PaymentAmount,0.0)) AS Decimal(20,2)) AS ReceivedPaymentAmount,
                Sum( Case WHEN IsNull(P.RemitMethod,'DATA') = 'DATA' THEN 0 ELSE 1 END ) As RequiredRemittanceAdviceFiles,
                Sum( Case WHEN IsNull(R.RemitMethod,'DATA') = 'DATA' THEN 0 ELSE 1 END ) As ReceivedRemittanceAdviceFiles  
    FROM        cteBatchesOfInterest as BoI
                LEFT JOIN dbo.InboundPayment AS P
                    ON BoI.InboundBatchId= P.InboundBatchId
                LEFT JOIN dbo.vwRemittanceAdviceFile AS R ON R.Payer835Id = P.Payer835Id AND R.UserKey=P.UserKey
    WHERE       /* These have been "fixed"  */
                CHARINDEX('_BAD',     IsNull( P.UserKey, '')) = 0 AND
                CHARINDEX('_DATASUP', IsNull( P.UserKey, '')) = 0 AND
                CHARINDEX('_PSTASK',  IsNull( P.UserKey, '')) = 0 AND
                CHARINDEX('_PROD',    IsNull( P.UserKey, '')) = 0 AND
                1=1
    GROUP BY    BoI.InboundBatchId 
)
SELECT      BoI.InboundBatchId,
            BoI.OptumBatchId,
            BoI.Payer835Id,
            HC.HeaderCount,
            BoI.TotalPaymentCount,
            PC.PaymentCount as ReceivedPaymentCount,
            BoI.TotalPaymentAmountIsNull,
            BoI.TotalPaymentAmount,
            PC.ReceivedPaymentAmount,
            PC.ReceivedRemittanceAdviceFiles,
            PC.RequiredRemittanceAdviceFiles,
            BoI.[Status],
            BoI.CreatedDateTime,
            BoI.LastModifiedDateTime--,
            /* ANY VALUE GREATER THAN ZERO FROM HERE DOWN IS AN ERROR CONDITION  */
            ,BoI.TotalPaymentAmountIsNull                                              AS ISSUE_NO_BATCHSUMMARY 
            ,CAST(ABS(BoI.TotalPaymentCount - PC.PaymentCount) AS INT)                 AS ISSUE_PMT_COUNT 
            ,CAST(CASE WHEN BoI.TotalPaymentAmountIsNull = 0 AND
                ABS(BoI.TotalPaymentAmount - PC.ReceivedPaymentAmount) > 0.0 THEN 1
                ELSE 0 END AS INT)                                                     AS ISSUE_PMT_AMOUNT 
             ,CAST(ABS(HC.HeaderCount - 1) AS INT)                                     AS ISSUE_HEADER 
             ,CAST( ABS(PC.ReceivedRemittanceAdviceFiles - PC.RequiredRemittanceAdviceFiles) AS INT) AS ISSUE_PDF_FILES
FROM        cteBatchesOfInterest AS BoI
            JOIN cteHeaderCount as HC
                ON HC.InboundBatchId=BoI.InboundBatchId
            JOIN ctePaymentCount as PC
                ON PC.InboundBatchId=BoI.InboundBatchId
GO