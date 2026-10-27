CREATE PROCEDURE [dbo].[usp_InboundPayment_GetByBatchIdWithoutClob]
   @InboundBatchId BIGINT
AS
BEGIN
SET NOCOUNT ON;

SELECT      
            P.[Id],
            P.[InboundBatchId],
            P.[UserKey],
            P.[Payer835Id],
            P.[FileProcId],
            P.[PayerAlias],
            P.[ProvTIN],
            P.[PayerSubpayerId],
            P.[ZipCode],
            P.[PaymentAmount],
            P.[CreatedDateTime],
            P.[Status],
            P.[LastModifiedDateTime],
            P.LastModifiedBy,
            IsNull(P.RemitMethod, 'DATA') AS RemitMethod
FROM        [dbo].[InboundPayment] AS P
WHERE       P.InboundBatchId = @InboundBatchId

END