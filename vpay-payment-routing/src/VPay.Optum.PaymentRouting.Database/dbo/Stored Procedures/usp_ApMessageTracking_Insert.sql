CREATE PROCEDURE [dbo].[usp_ApMessageTracking_Insert]
     @ApMessageId bigint,
     @InboundPaymentId bigint,
     @TransactionId bigint,
     @TrnSegment varchar(256),
     @LoadAmount decimal(20,2),
     @PaymentTypeCode varchar(10),
     @CheckNumber varchar(10) null,
     @LoadDate DateTime2(7),
     @Status varchar(20),
     @CreatedBy varchar(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        DECLARE @ApMessageTrackingId bigint;

        INSERT [dbo].[ApMessageTracking] (ApMessageId, InboundPaymentId, TransactionId, TrnSegment, LoadAmount, PaymentTypeCode, CheckNumber, LoadDate, [Status], CreatedBy, CreatedDateTime, LastModifiedBy, LastModifiedDateTime)
        SELECT @ApMessageId, @InboundPaymentId, @TransactionId, @TrnSegment, @LoadAmount, @PaymentTypeCode, @CheckNumber, @LoadDate, @Status, @CreatedBy, SYSDATETIME(), @CreatedBy, SYSDATETIME();

        SET @ApMessageTrackingId=SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH

    SELECT @ApMessageTrackingId as ApMessageTrackingId;
    RETURN @ApMessageTrackingId;
END