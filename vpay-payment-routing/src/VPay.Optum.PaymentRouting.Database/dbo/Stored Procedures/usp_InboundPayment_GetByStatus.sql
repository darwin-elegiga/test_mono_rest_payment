CREATE PROCEDURE [dbo].[usp_InboundPayment_GetByStatus]
    @Status varchar(20)
AS
BEGIN
SET NOCOUNT ON;

SELECT      B.[Id] AS InboundBatchId,
            P.[Id] AS InboundPaymentId,
            B.[OptumBatchId],
            P.[UserKey],
            P.[Payer835Id],
            P.[FileProcId],
            P.[PayerAlias],
            P.[ProvTIN],
            P.[PayerSubpayerId],
            P.[ZipCode],
            P.[PaymentAmount],
            B.[CreatedDateTime] AS BatchCreatedDateTime,
            P.[CreatedDateTime] AS PaymentCreatedDateTime,
            C.PaymentClob,
            P.[Status],
            P.[OutboundRefEo],
            P.[LastModifiedDateTime],
            P.LastModifiedBy,
            IsNull(P.RemitMethod, 'DATA') AS RemitMethod
FROM        [dbo].[InboundBatch] AS B
            INNER JOIN [dbo].[InboundPayment] AS P
                ON P.InboundBatchId=B.Id
            INNER JOIN [dbo].ClobStorage AS C
                ON P.[ClobStorageId]=C.[Id]
WHERE       P.[Status] = @Status;

END