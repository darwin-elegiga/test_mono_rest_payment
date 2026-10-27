CREATE PROCEDURE [dbo].[usp_OutboundPaymentClob_Insert]
    @InboundPaymentId BIGINT, 
    @Clob VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON
    INSERT dbo.OutboundPaymentClob (InboundPaymentId, Clob)
    SELECT @InboundPaymentId, @Clob;
END