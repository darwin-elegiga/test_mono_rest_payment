CREATE PROCEDURE [dbo].[usp_ApMessageTracking_GetByInboundPaymentId]
     @InboundPaymentId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  Id AS ApMessageTrackingId,
            ApMessageId,
            InboundPaymentId,
            TransactionId,
            TrnSegment,
            LoadAmount,
            PaymentTypeCode,
            CheckNumber,
            LoadDate,
            [Status],
            CreatedBy,
            CreatedDateTime,
            LastModifiedBy,
            LastModifiedDateTime
    FROM    [dbo].[ApMessageTracking]
    WHERE   InboundPaymentId = @InboundPaymentId;

END