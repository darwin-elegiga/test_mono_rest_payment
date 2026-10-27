CREATE PROCEDURE [dbo].[usp_InboundPayment_GetBy_UserKey_Payer835Id]
    @UserKey VARCHAR(100),
    @Payer835Id VARCHAR(5)
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
            P.[LastModifiedDateTime],
            P.LastModifiedBy,
            IsNull(P.RemitMethod, 'DATA') AS RemitMethod
FROM        [dbo].[InboundBatch] AS B
            INNER JOIN [dbo].[InboundPayment] AS P
                ON P.InboundBatchId=B.Id
            INNER JOIN [dbo].ClobStorage AS C
                ON P.[ClobStorageId]=C.[Id]
WHERE       P.[UserKey] = @UserKey
            AND P.[Payer835Id] = @Payer835Id;

END
