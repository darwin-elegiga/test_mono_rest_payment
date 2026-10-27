CREATE PROCEDURE dbo.[usp_InboundBatch_CancelAllPayments](
    @InboundBatchId BIGINT,
    @JiraTicket VARCHAR(100)
)
AS
BEGIN 
UPDATE		P
SET			P.UserKey = P.UserKey + '_' + @JiraTicket,
            P.Status = 'CANCELLED',
            P.LastModifiedBy = @JiraTicket,
            P.LastModifiedDateTime = SYSDATETIME()
FROM		dbo.InboundPayment AS P
            JOIN dbo.InboundBatch AS B ON P.InboundBatchId=B.Id
WHERE		B.[Status] = 'BatchInitiated' 
            AND B.Id = @InboundBatchId
            AND Right( P.UserKey, Len(@JiraTicket) ) != @JiraTicket;
 
UPDATE		B
SET			B.Status = 'BatchCancelled',
            B.LastModifiedDateTime  = SYSDATETIME()

FROM		dbo.InboundBatch B 
WHERE		B.[Status] = 'BatchInitiated'
            AND B.Id = @InboundBatchId;

END