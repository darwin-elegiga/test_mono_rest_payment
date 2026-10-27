CREATE PROCEDURE [dbo].[usp_ApMessageTracking_GetById]
     @Id bigint
AS
BEGIN
SET NOCOUNT ON;

SELECT  Id as ApMessageTrackingId,
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
WHERE   Id = @Id;

END
