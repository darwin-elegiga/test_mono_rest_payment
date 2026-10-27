CREATE PROCEDURE [dbo].[usp_InboundBatch_GetManifest]
     @InboundBatchId BIGINT 
AS
BEGIN
SET NOCOUNT ON;

SELECT      B.[Id] AS InboundBatchId,
            H.[Id] AS InboundHeaderId,
            B.[OptumBatchId],
            B.[Payer835Id],
            H.[PayerSubpayerId],
            H.[PayerAlias],
            B.[TotalPaymentCount],
            B.[TotalPaymentAmount],
            B.[Status],
            B.[CreatedDateTime] AS BatchCreatedDateTime,
            H.[CreatedDateTime] AS HeaderCreatedDateTime,
            C.PaymentClob AS HeaderData
FROM        [dbo].[InboundBatch] AS B
            INNER JOIN [dbo].[InboundHeader] AS H
                ON H.InboundBatchId=B.Id
            INNER JOIN [dbo].ClobStorage AS C
                ON H.[ClobStorageId]=C.[Id]
WHERE       B.[Id] = @InboundBatchId;

SELECT      P.[Id] AS InboundPaymentId 
FROM        [dbo].[InboundPayment] AS P
WHERE       P.[InboundBatchId] = @InboundBatchId;


END