CREATE PROCEDURE [dbo].[usp_InboundHeader_GetById]
    @InboundHeaderId BIGINT
AS
BEGIN
SET NOCOUNT ON;

SELECT      B.[Id] AS InboundBatchId,
            E.[Id] AS InboundHeaderId,
            B.[OptumBatchId],
            B.[Payer835Id],
            E.[PayerSubpayerId],
            E.[PayerAlias],
            B.[TotalPaymentCount],
            B.[TotalPaymentAmount],
            B.[CreatedDateTime] AS BatchCreatedDateTime,
            E.[CreatedDateTime] AS HeaderCreatedDateTime,
            C.PaymentClob AS HeaderData,
            E.[Status],
            E.[LastModifiedDateTime],
            E.LastModifiedBy
FROM        [dbo].[InboundBatch] AS B
            INNER JOIN [dbo].[InboundHeader] AS E
                ON E.InboundBatchId=B.Id
            INNER JOIN [dbo].ClobStorage AS C
                ON E.[ClobStorageId]=C.[Id]
WHERE       E.[Id] = @InboundHeaderId;
END