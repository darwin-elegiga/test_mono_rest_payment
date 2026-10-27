CREATE PROCEDURE [dbo].[usp_InboundHeader_GetByBatchId]
    @InboundBatchId BIGINT
AS
BEGIN
SET NOCOUNT ON;

SELECT      E.[Id] AS InboundHeaderId,
            E.[PayerSubpayerId],
            E.[PayerAlias],
            C.PaymentClob AS HeaderData,
            E.[Status]
FROM        [dbo].[InboundBatch] AS B
            INNER JOIN [dbo].[InboundHeader] AS E
                ON E.InboundBatchId=B.Id
            INNER JOIN [dbo].ClobStorage AS C
                ON E.[ClobStorageId]=C.[Id]
WHERE       E.[InboundBatchId] = @InboundBatchId;
END