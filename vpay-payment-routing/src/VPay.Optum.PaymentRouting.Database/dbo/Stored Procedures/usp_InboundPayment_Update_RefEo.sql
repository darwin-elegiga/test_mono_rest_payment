CREATE PROCEDURE [dbo].[usp_InboundPayment_Update_RefEo]
    @InboundPaymentId   BIGINT, 
    @OutboundRefEo      VARCHAR(10),
    @ModifiedBy         VARCHAR(50)
AS
BEGIN
SET NOCOUNT OFF; -- We want the number of rows affected returned.
    UPDATE      dbo.InboundPayment
    SET         OutboundRefEo = @OutboundRefEo,
                LastModifiedBy = @ModifiedBy,
                LastModifiedDateTime = SYSDATETIME()
    WHERE       Id = @InboundPaymentId;
END