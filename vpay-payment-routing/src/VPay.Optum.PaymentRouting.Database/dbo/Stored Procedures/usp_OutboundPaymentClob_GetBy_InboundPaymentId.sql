CREATE PROCEDURE [dbo].[usp_OutboundPaymentClob_GetBy_InboundPaymentId]
    @InboundPaymentId BIGINT
AS
BEGIN
    select Id,
        InboundPaymentId,
        Clob,
        CreatedDateTime
    from OutboundPaymentClob
    where InboundPaymentId = @InboundPaymentId
END