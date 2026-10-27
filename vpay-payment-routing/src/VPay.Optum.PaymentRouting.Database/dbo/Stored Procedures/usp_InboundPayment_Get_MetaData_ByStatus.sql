CREATE PROCEDURE [dbo].[usp_InboundPayment_Get_MetaData_ByStatus]
    @Status varchar(20)
AS
BEGIN
-- Very lightweight method to get the needed data by the process
-- to build the files.
SET NOCOUNT ON;

SELECT      P.[Id] AS InboundPaymentId,
            P.[PayerAlias],
            P.[OutboundRefEo],
            P.[UserKey],
            P.[PaymentAmount],
            P.[Status],
            IsNull(P.RemitMethod, 'DATA') AS RemitMethod 
FROM        [dbo].[InboundPayment] AS P WITH (NOLOCK) 
WHERE       P.[Status] = @Status;

END